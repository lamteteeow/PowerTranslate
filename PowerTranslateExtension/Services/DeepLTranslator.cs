using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerTranslateExtension.Services;

internal sealed class DeepLTranslator
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    private static readonly object LanguageChoicesCacheLock = new();
    private static (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices)? _cachedLanguageChoices;
    private const int MaxRetries = 3;

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

    public static (List<ChoiceSetSetting.Choice> SourceChoices, List<ChoiceSetSetting.Choice> TargetChoices) GetCachedSupportedLanguageChoices()
    {
        lock (LanguageChoicesCacheLock)
        {
            if (_cachedLanguageChoices is { } cached)
            {
                return CloneLanguageChoices(cached);
            }
        }

        return EmptyLanguageChoices();
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
        RuntimeLog.Info($"Translate requested. source={sourceLanguage}, target={targetLanguage}, inputLength={(input ?? string.Empty).Length}");

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
            HttpResponseMessage? response = null;
            string? responseText = null;

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
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

                    response = Client.SendAsync(request).GetAwaiter().GetResult();
                    responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    break;
                }
                catch (HttpRequestException) when (attempt < MaxRetries)
                {
                    Thread.Sleep(500 * (attempt + 1));
                    continue;
                }
            }

            if (response == null || responseText == null)
            {
                RuntimeLog.Error("Translate failed: no response after retries.");
                return new TranslationResult(false, "Network error: Unable to connect to DeepL after multiple attempts. Check your internet connection.");
            }

            if (!response.IsSuccessStatusCode)
            {
                RuntimeLog.Error($"Translate failed with status {(int)response.StatusCode}.");
                return GetFriendlyErrorMessage(response.StatusCode, responseText);
            }

            using var json = JsonDocument.Parse(responseText);
            if (!json.RootElement.TryGetProperty("translations", out var translations) || translations.GetArrayLength() == 0)
            {
                RuntimeLog.Error("Translate failed: response missing translations array.");
                return new TranslationResult(false, "DeepL returned an unexpected response. Try again or check your API key.");
            }

            var translation = translations[0];
            var translatedText = translation.GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(translatedText))
            {
                RuntimeLog.Error("Translate failed: empty translated text from DeepL.");
                return new TranslationResult(false, "DeepL returned an empty translation. Try with different text.");
            }

            var detectedSource = translation.TryGetProperty("detected_source_language", out var detectedNode)
                ? NormalizeLanguageCode(detectedNode.GetString() ?? source)
                : source;

            var sourceForDisplay = string.Equals(source, "AUTO", StringComparison.Ordinal)
                ? detectedSource
                : source;

            RuntimeLog.Info($"Translate succeeded. source={sourceForDisplay}, target={target}, outputLength={translatedText.Length}");

            return new TranslationResult(true, translatedText, sourceForDisplay, target);
        }
        catch (HttpRequestException ex)
        {
            RuntimeLog.Error("Translate network exception.", ex);
            var message = ex.InnerException?.Message ?? ex.Message;
            return new TranslationResult(false, $"Network error: Unable to reach DeepL. Check your internet connection and firewall settings. Details: {message}");
        }
        catch (OperationCanceledException)
        {
            RuntimeLog.Error("Translate timed out.");
            return new TranslationResult(false, "Request timed out (5 seconds). The DeepL server is not responding. Check your internet connection or try again in a moment.");
        }
        catch (JsonException)
        {
            RuntimeLog.Error("Translate failed: invalid JSON returned by DeepL.");
            return new TranslationResult(false, "DeepL returned invalid data. Try again in a moment.");
        }
        catch (Exception ex)
        {
            RuntimeLog.Error("Translate unexpected exception.", ex);
            return new TranslationResult(false, $"Unexpected error: {ex.Message}");
        }
    }

    private static TranslationResult GetFriendlyErrorMessage(System.Net.HttpStatusCode statusCode, string responseText)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                new TranslationResult(false, "Invalid DeepL API key. Check your credentials."),
            System.Net.HttpStatusCode.TooManyRequests =>
                new TranslationResult(false, "DeepL request limit exceeded. Please wait a moment and try again."),
            System.Net.HttpStatusCode.BadRequest =>
                new TranslationResult(false, "Invalid request to DeepL. Check your language selection and try again."),
            System.Net.HttpStatusCode.ServiceUnavailable =>
                new TranslationResult(false, "DeepL service is temporarily unavailable. Try again in a moment."),
            _ =>
                new TranslationResult(false, $"DeepL error ({(int)statusCode}). If this persists, check your API key.")
        };
    }

    public TranslationResult CheckConnection()
    {
        RuntimeLog.Info("CheckConnection requested.");

        var apiKey = _settingsStore.GetDeepLApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            RuntimeLog.Error("CheckConnection failed: API key not set.");
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
                RuntimeLog.Error($"CheckConnection failed with status {(int)response.StatusCode}.");
                return GetFriendlyErrorMessage(response.StatusCode, responseText);
            }

            using var json = JsonDocument.Parse(responseText);
            var used = json.RootElement.TryGetProperty("character_count", out var usedNode) ? usedNode.GetInt32() : -1;
            var limit = json.RootElement.TryGetProperty("character_limit", out var limitNode) ? limitNode.GetInt32() : -1;
            if (used >= 0 && limit > 0)
            {
                RuntimeLog.Info($"CheckConnection succeeded. usage={used}/{limit}");
                return new TranslationResult(true, $"DeepL connection OK. Usage {used}/{limit} characters.");
            }

            RuntimeLog.Info("CheckConnection succeeded.");
            return new TranslationResult(true, "DeepL connection OK.");
        }
        catch (HttpRequestException ex)
        {
            RuntimeLog.Error("CheckConnection network exception.", ex);
            var message = ex.InnerException?.Message ?? ex.Message;
            return new TranslationResult(false, $"Network error: Unable to reach DeepL. Check your internet connection. Details: {message}");
        }
        catch (OperationCanceledException)
        {
            RuntimeLog.Error("CheckConnection timed out.");
            return new TranslationResult(false, "Connection timed out (5 seconds). DeepL server is not responding. Check your internet connection and try again.");
        }
        catch (JsonException)
        {
            RuntimeLog.Error("CheckConnection failed: invalid JSON returned by DeepL.");
            return new TranslationResult(false, "DeepL returned invalid data.");
        }
        catch (Exception ex)
        {
            RuntimeLog.Error("CheckConnection unexpected exception.", ex);
            return new TranslationResult(false, $"Unexpected error: {ex.Message}");
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

internal readonly record struct TranslationResult(bool IsSuccess, string Message, string? SourceLanguage = null, string? TargetLanguage = null);
internal readonly record struct DeepLLanguage(string Code, string Name);
