using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PowerTranslateExtension.Services;

internal sealed class DeepLTranslator
{
    private static readonly HttpClient Client = new();

    private readonly LocalSettingsStore _settingsStore;

    public DeepLTranslator(LocalSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
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
}

internal readonly record struct TranslationResult(bool IsSuccess, string Message);
