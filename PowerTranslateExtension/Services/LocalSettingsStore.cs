using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PowerTranslateExtension.Services;

internal sealed class LocalSettingsStore
{
    private const string ApiKeyFileName = "deepl.key";
    private const string SourceLanguageFileName = "source-language.txt";
    private const string TargetLanguageFileName = "target-language.txt";
    private const string DefaultSourceLanguage = "AUTO";
    private const string DefaultTargetLanguage = "EN";

    private readonly string _settingsDirectory;
    private readonly string _apiKeyPath;
    private readonly string _sourceLanguagePath;
    private readonly string _targetLanguagePath;

    public LocalSettingsStore()
    {
        _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PowerTranslateExtension");
        _apiKeyPath = Path.Combine(_settingsDirectory, ApiKeyFileName);
        _sourceLanguagePath = Path.Combine(_settingsDirectory, SourceLanguageFileName);
        _targetLanguagePath = Path.Combine(_settingsDirectory, TargetLanguageFileName);
    }

    public string? GetDeepLApiKey()
    {
        if (!File.Exists(_apiKeyPath))
        {
            return null;
        }

        try
        {
            var encrypted = File.ReadAllBytes(_apiKeyPath);
            var plaintext = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return null;
        }
    }

    public void SaveDeepLApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (File.Exists(_apiKeyPath))
            {
                File.Delete(_apiKeyPath);
            }

            return;
        }

        Directory.CreateDirectory(_settingsDirectory);

        var plaintext = Encoding.UTF8.GetBytes(apiKey.Trim());
        var encrypted = ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_apiKeyPath, encrypted);
    }

    public string GetSourceLanguage()
    {
        return ReadLanguageOrDefault(_sourceLanguagePath, DefaultSourceLanguage);
    }

    public string GetTargetLanguage()
    {
        return ReadLanguageOrDefault(_targetLanguagePath, DefaultTargetLanguage);
    }

    public void SaveSourceLanguage(string? languageCode)
    {
        SaveLanguage(_sourceLanguagePath, languageCode, DefaultSourceLanguage);
    }

    public void SaveTargetLanguage(string? languageCode)
    {
        SaveLanguage(_targetLanguagePath, languageCode, DefaultTargetLanguage);
    }

    private string ReadLanguageOrDefault(string path, string fallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            var value = File.ReadAllText(path).Trim().ToUpperInvariant();
            return IsSupportedLanguage(value) ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private void SaveLanguage(string path, string? languageCode, string fallback)
    {
        Directory.CreateDirectory(_settingsDirectory);

        var normalized = (languageCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsSupportedLanguage(normalized))
        {
            normalized = fallback;
        }

        File.WriteAllText(path, normalized);
    }

    private static bool IsSupportedLanguage(string languageCode)
    {
        return languageCode is "AUTO" or "EN" or "DE" or "VN";
    }
}
