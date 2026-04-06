using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslateExtension.Services;

internal sealed class DeepLTranslator
{
    private static readonly HttpClient Client = new();
    private static readonly object LanguageChoicesCacheLock = new();
    private static (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices)? _cachedLanguageChoices;

    private readonly LocalSettingsStore _settingsStore;

    public DeepLTranslator(LocalSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) GetSupportedLanguageChoices(bool forceReload = false)
    {
        lock (LanguageChoicesCacheLock)
        {
            if (!forceReload && _cachedLanguageChoices is { } cached)
            {
                return CloneLanguageChoices(cached);
            }
        }

        var refreshedChoices = BuildSupportedLanguageChoices();

        lock (LanguageChoicesCacheLock)
        {
            if (refreshedChoices.SourceChoices.Count == 0 || refreshedChoices.TargetChoices.Count == 0)
            {
                // If API fetch fails, deactivate language selection by clearing cache and returning no choices.
                _cachedLanguageChoices = null;
                return EmptyLanguageChoices();
            }

            _cachedLanguageChoices = CloneLanguageChoices(refreshedChoices);
            return CloneLanguageChoices(_cachedLanguageChoices.Value);
        }
    }

    public (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) ReloadSupportedLanguageChoices()
    {
        return GetSupportedLanguageChoices(forceReload: true);
    }

    private (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) BuildSupportedLanguageChoices()
    {
        var apiKey = _settingsStore.GetDeepLApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return EmptyLanguageChoices();
        }

        var baseUrl = GetApiBaseUrl(apiKey);

        try
        {
            var sourceLanguages = FetchSupportedLanguages(apiKey, baseUrl + "/v2/languages?type=source");
            var targetLanguages = FetchSupportedLanguages(apiKey, baseUrl + "/v2/languages?type=target");

            if (sourceLanguages.Count == 0 || targetLanguages.Count == 0)
            {
                return EmptyLanguageChoices();
            }

            var sourceChoices = new List<ChoiceSetSetting.Choice>
            {
                new("Auto", "AUTO"),
            };

            sourceChoices.AddRange(sourceLanguages.Select(l => new ChoiceSetSetting.Choice(l.Name, l.Code)));

            var targetChoices = targetLanguages
                .Select(l => new ChoiceSetSetting.Choice(l.Name, l.Code))
                .ToList();

            return (sourceChoices, targetChoices);
        }
        catch
        {
            return EmptyLanguageChoices();
        }
    }

    private static (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) CloneLanguageChoices((List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) choices)
    {
        var source = choices.SourceChoices.Select(c => new ChoiceSetSetting.Choice(c.Title, c.Value)).ToList();
        var target = choices.TargetChoices.Select(c => new ChoiceSetSetting.Choice(c.Title, c.Value)).ToList();
        return (source, target);
    }

    public TranslationResult Translate(string input, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new TranslationResult(false, "Enter text to translate.");
        }

        if (string.IsNullOrWhiteSpace(sourceLanguage))
        {
            return new TranslationResult(false, "Select a valid input language.");
        }

        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            return new TranslationResult(false, "Select a valid target language.");
        }

        var source = NormalizeLanguageCode(sourceLanguage);
        var target = NormalizeLanguageCode(targetLanguage);
        if (string.Equals(source, target, StringComparison.Ordinal))
        {
            return new TranslationResult(false, "Input and target languages must be different.");
        }

        var apiKey = _settingsStore.GetDeepLApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslationResult(false, "DeepL API key is not set. Open 'Configure DeepL API key' and save your key.");
        }

        var endpoint = GetApiBaseUrl(apiKey) + "/v2/translate";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);
            var requestFields = new List<KeyValuePair<string, string>>
            {
                new("text", input.Trim()),
                new("target_lang", target)
            };

            if (!string.Equals(source, "AUTO", StringComparison.OrdinalIgnoreCase))
            {
                requestFields.Add(new KeyValuePair<string, string>("source_lang", source));
            }

            request.Content = new FormUrlEncodedContent(requestFields);

            using var response = Client.SendAsync(request).GetAwaiter().GetResult();
            var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return new TranslationResult(false, $"DeepL request failed ({(int)response.StatusCode}): {responseText}");
            }

            using var json = JsonDocument.Parse(responseText);
            if (!json.RootElement.TryGetProperty("translations", out var translations) || translations.GetArrayLength() == 0)
            {
                return new TranslationResult(false, "DeepL returned an unexpected response.");
            }

            var translatedText = translations[0].GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(translatedText))
            {
                return new TranslationResult(false, "DeepL returned an empty translation.");
            }

            return new TranslationResult(true, translatedText);
        }
        catch (Exception ex)
        {
            return new TranslationResult(false, $"DeepL error: {ex.Message}");
        }
    }

    public TranslationResult CheckConnection()
    {
        var apiKey = _settingsStore.GetDeepLApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslationResult(false, "DeepL API key is not set.");
        }

        var endpoint = GetApiBaseUrl(apiKey) + "/v2/usage";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);

            using var response = Client.SendAsync(request).GetAwaiter().GetResult();
            var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return new TranslationResult(false, $"Diagnostic failed ({(int)response.StatusCode}): {responseText}");
            }

            using var json = JsonDocument.Parse(responseText);
            var used = json.RootElement.TryGetProperty("character_count", out var usedNode) ? usedNode.GetInt32() : -1;
            var limit = json.RootElement.TryGetProperty("character_limit", out var limitNode) ? limitNode.GetInt32() : -1;
            if (used >= 0 && limit > 0)
            {
                return new TranslationResult(true, $"DeepL connection OK. Usage {used}/{limit} characters.");
            }

            return new TranslationResult(true, "DeepL connection OK.");
        }
        catch (Exception ex)
        {
            return new TranslationResult(false, $"DeepL diagnostic error: {ex.Message}");
        }
    }

    private static string GetApiBaseUrl(string apiKey)
    {
        return apiKey.Contains(":fx", StringComparison.OrdinalIgnoreCase)
            ? "https://api-free.deepl.com"
            : "https://api.deepl.com";
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        var normalized = languageCode.Trim().ToUpperInvariant();
        return normalized switch
        {
            "VN" => "VI",
            _ => normalized,
        };
    }

    private static List<DeepLLanguage> FetchSupportedLanguages(string apiKey, string endpoint)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);

        using var response = Client.SendAsync(request).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var json = JsonDocument.Parse(responseText);

        if (json.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var languages = new List<DeepLLanguage>();
        foreach (var item in json.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("language", out var codeNode))
            {
                continue;
            }

            var rawCode = codeNode.GetString();
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                continue;
            }

            var code = NormalizeLanguageCode(rawCode);
            var name = item.TryGetProperty("name", out var nameNode) && !string.IsNullOrWhiteSpace(nameNode.GetString())
                ? nameNode.GetString()!
                : code;

            languages.Add(new DeepLLanguage(code, name));
        }

        return languages
            .GroupBy(l => l.Code, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) EmptyLanguageChoices()
    {
        return ([], []);
    }
}

internal readonly record struct TranslationResult(bool IsSuccess, string Message);
internal readonly record struct DeepLLanguage(string Code, string Name);
