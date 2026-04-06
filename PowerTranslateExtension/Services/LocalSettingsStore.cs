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
        return ReadLanguageOrDefault(_sourceLanguagePath, DefaultSourceLanguage, allowAuto: true);
    }

    public string GetTargetLanguage()
    {
        return ReadLanguageOrDefault(_targetLanguagePath, DefaultTargetLanguage, allowAuto: false);
    }

    public void SaveSourceLanguage(string? languageCode)
    {
        SaveLanguage(_sourceLanguagePath, languageCode, DefaultSourceLanguage, allowAuto: true);
    }

    public void SaveTargetLanguage(string? languageCode)
    {
        SaveLanguage(_targetLanguagePath, languageCode, DefaultTargetLanguage, allowAuto: false);
    }

    private static string ReadLanguageOrDefault(string path, string fallback, bool allowAuto)
    {
        try
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            var value = NormalizeLanguageCode(File.ReadAllText(path));
            return IsSupportedLanguage(value, allowAuto) ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private void SaveLanguage(string path, string? languageCode, string fallback, bool allowAuto)
    {
        Directory.CreateDirectory(_settingsDirectory);

        var normalized = NormalizeLanguageCode(languageCode);
        if (!IsSupportedLanguage(normalized, allowAuto))
        {
            normalized = fallback;
        }

        File.WriteAllText(path, normalized);
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        var normalized = (languageCode ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "VN" => "VI",
            _ => normalized,
        };
    }

    private static bool IsSupportedLanguage(string languageCode, bool allowAuto)
    {
        if (allowAuto && languageCode == "AUTO")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return false;
        }

        foreach (var c in languageCode)
        {
            if ((c < 'A' || c > 'Z') && c != '-')
            {
                return false;
            }
        }

        return languageCode.Length <= 15;
    }
}
