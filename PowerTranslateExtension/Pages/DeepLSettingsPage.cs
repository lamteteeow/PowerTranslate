using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslateExtension.Services;
using System.Text.Json.Nodes;

namespace PowerTranslateExtension;

internal sealed partial class DeepLSettingsPage : ContentPage
{
    private readonly LocalSettingsStore _settingsStore = new();
    private readonly DeepLTranslator _translator;
    private readonly ApiKeyForm _form;
    private readonly MarkdownContent _statusContent = new();

    public DeepLSettingsPage()
    {
        _translator = new DeepLTranslator(_settingsStore);
        _form = new ApiKeyForm(this);
        Icon = IconHelpers.FromRelativePath("Assets\\PowerTranslateLogo.png");
        Title = "DeepL Settings";
        UpdateStatusBody("Enter API key and select Save.");
    }

    public override IContent[] GetContent()
    {
        return [_statusContent, _form];
    }

    private CommandResult SaveApiKey(string? rawValue)
    {
        var candidateKey = (rawValue ?? string.Empty).Trim();
        var previousKey = _settingsStore.GetDeepLApiKey();

        if (string.IsNullOrWhiteSpace(candidateKey))
        {
            if (!string.IsNullOrWhiteSpace(previousKey))
            {
                _translator.ReloadSupportedLanguageChoices();
                UpdateStatusBody("Using previously saved API key. Settings unchanged.");
                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = "Using previously saved API key.",
                    Result = CommandResult.KeepOpen(),
                });
            }

            UpdateStatusBody("API key is not set.");
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
                UpdateStatusBody("API is invalid.");
                return CommandResult.ShowToast(new ToastArgs
                {
                    Message = "API is invalid.",
                    Result = CommandResult.KeepOpen(),
                });
            }

            _translator.ReloadSupportedLanguageChoices();

            UpdateStatusBody("API is valid. Settings saved.");
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = "API is valid. Settings saved.",
                Result = CommandResult.KeepOpen(),
            });
        }

        UpdateStatusBody("Settings saved.");
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
        UpdateStatusBody("API key cleared.");
        return CommandResult.ShowToast(new ToastArgs { Message = "Settings saved.", Result = CommandResult.KeepOpen() });
    }

    private void UpdateStatusBody(string message)
    {
        var maskedCurrentKey = MaskKey(_settingsStore.GetDeepLApiKey());
        var maskedLine = string.IsNullOrEmpty(maskedCurrentKey) ? "Not set" : maskedCurrentKey;
        _statusContent.Body =
            $"Current API key: {maskedLine}\n\n{message}\n\nTip: After setting the API key, reload Command Palette extensions to refresh language choices.";
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
            TemplateJson = """
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.6",
  "body": [
    {
      "type": "Input.Text",
      "id": "apiKey",
      "label": "DeepL API key",
      "placeholder": "Paste DeepL API key",
      "style": "password",
      "isMultiline": false,
            "isRequired": false
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
            DataJson = "{}";
        }

        public override CommandResult SubmitForm(string payload)
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
    }
}
