using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslateExtension.Services;
using System;
using System.Text.Json.Nodes;

namespace PowerTranslateExtension;

internal sealed partial class DeepLSettingsPage : ContentPage
{
    private readonly LocalSettingsStore _settingsStore = new();
    private readonly DeepLTranslator _translator;
    private readonly ApiKeyForm _form;

    public DeepLSettingsPage()
    {
        _translator = new DeepLTranslator(_settingsStore);
        _form = new ApiKeyForm(this);
        Icon = IconHelpers.FromRelativePath("Assets\\PowerTranslateLogo.png");
        Title = "DeepL Settings";
        UpdateStatusBody("Enter API key and select Save.", isError: false);
    }

    public override IContent[] GetContent()
    {
        return [_form];
    }

    private CommandResult SaveApiKey(string? rawValue)
    {
        var candidateKey = (rawValue ?? string.Empty).Trim();
        var previousKey = _settingsStore.GetDeepLApiKey();

        RuntimeLog.Info($"SaveApiKey requested. hasInput={!string.IsNullOrWhiteSpace(candidateKey)}");

        if (string.IsNullOrWhiteSpace(candidateKey))
        {
            if (!string.IsNullOrWhiteSpace(previousKey))
            {
                _translator.ReloadSupportedLanguageChoices();
                RuntimeLog.Info("SaveApiKey: no new key provided; keeping existing key.");
                UpdateStatusBody("Using previously saved API key. Settings unchanged.", isError: false);
                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = "Using previously saved API key.",
                    Result = CommandResult.KeepOpen(),
                });
            }

            UpdateStatusBody("API key is not set.", isError: true);
            RuntimeLog.Error("SaveApiKey failed: no key provided and no existing key.");
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = "API key is not set.",
                Result = CommandResult.KeepOpen(),
            });
        }

        _settingsStore.SaveDeepLApiKey(candidateKey);

        if (!string.IsNullOrWhiteSpace(candidateKey))
        {
            var connectionResult = _translator.CheckConnection();
            if (!connectionResult.IsSuccess)
            {
                _settingsStore.SaveDeepLApiKey(previousKey);
                RuntimeLog.Error($"SaveApiKey validation failed: {connectionResult.Message}");
                UpdateStatusBody($"{connectionResult.Message}", isError: true);
                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = $"Failed to validate: {connectionResult.Message}",
                    Result = CommandResult.KeepOpen(),
                });
            }

            _translator.ReloadSupportedLanguageChoices();

            RuntimeLog.Info("SaveApiKey succeeded.");
            UpdateStatusBody("API is valid. Settings saved.", isError: false);
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = "API is valid. Settings saved.",
                Result = CommandResult.KeepOpen(),
            });
        }

        UpdateStatusBody("Settings saved.", isError: false);
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = "Settings saved.",
            Result = CommandResult.KeepOpen(),
        });
    }

    private CommandResult ClearApiKey()
    {
        _settingsStore.SaveDeepLApiKey(null);
        _translator.ReloadSupportedLanguageChoices();
        RuntimeLog.Info("API key cleared.");
        UpdateStatusBody("API key cleared.", isError: false);
        return CommandResult.ShowToast(new ToastArgs { Message = "Settings saved.", Result = CommandResult.KeepOpen() });
    }

    private void UpdateStatusBody(string message, bool isError)
    {
        var maskedCurrentKey = MaskKey(_settingsStore.GetDeepLApiKey());
        var maskedLine = string.IsNullOrEmpty(maskedCurrentKey) ? "Not set" : maskedCurrentKey;
        _form.UpdateStatus(maskedLine, message, isError);
        RaiseItemsChanged(1);
    }

    private static string MaskKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string('*', value.Length);
    }

    private sealed partial class ApiKeyForm : FormContent
    {
        private readonly DeepLSettingsPage _owner;

        public ApiKeyForm(DeepLSettingsPage owner)
        {
            _owner = owner;
            DataJson = "{}";
            UpdateStatus("Not set", "Enter API key and select Save.", isError: false);
        }

        public void UpdateStatus(string maskedApiKey, string message, bool isError)
        {
            var safeKey = JsonEncoded(maskedApiKey);
            var safeMessage = JsonEncoded(message);
            var color = isError ? "Attention" : "Good";

            TemplateJson = $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "text": "Current API key: {{safeKey}}",
            "wrap": true,
            "spacing": "None"
        },
        {
            "type": "TextBlock",
            "text": "{{safeMessage}}",
            "wrap": true,
            "color": "{{color}}",
            "weight": "Bolder",
            "spacing": "Small"
        },
        {
            "type": "TextBlock",
            "text": "Tip: After setting the API key, reload Command Palette extensions to refresh language choices.",
            "wrap": true,
            "isSubtle": true,
            "spacing": "Small"
        },
        {
            "type": "Input.Text",
            "id": "apiKey",
            "label": "DeepL API key",
            "placeholder": "Paste DeepL API key",
            "style": "password",
            "isMultiline": false,
            "isRequired": false,
            "spacing": "Medium"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Save",
            "data": { "action": "save" }
        },
        {
            "type": "Action.Submit",
            "title": "Clear",
            "data": { "action": "clear" }
        }
    ]
}
""";
        }

        private static string JsonEncoded(string value)
        {
            var input = value ?? string.Empty;
            return input
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", string.Empty)
                    .Replace("\n", "\\n");
        }

        public override CommandResult SubmitForm(string payload)
        {
            try
            {
                var formInput = JsonNode.Parse(payload)?.AsObject();
                var action = formInput?["action"]?.ToString();

                if (string.Equals(action, "clear", System.StringComparison.OrdinalIgnoreCase))
                {
                    return _owner.ClearApiKey();
                }

                var apiKey = formInput?["apiKey"]?.ToString();
                return _owner.SaveApiKey(apiKey);
            }
            catch (Exception ex)
            {
                RuntimeLog.Error("DeepL settings form submit failed.", ex);
                _owner.UpdateStatusBody("Failed to save settings due to an unexpected error.", isError: true);
                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = "Failed to save settings. Please try again.",
                    Result = CommandResult.KeepOpen(),
                });
            }
        }
    }
}
