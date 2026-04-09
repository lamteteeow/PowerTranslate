// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslateExtension.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerTranslateExtension;

public partial class PowerTranslateExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private const string SourceLanguageSettingKey = "DeepL.SourceLanguage";
    private const string TargetLanguageSettingKey = "DeepL.TargetLanguage";

    public PowerTranslateExtensionCommandsProvider()
    {
        var settingsStore = new LocalSettingsStore();
        var settings = new Settings();

        try
        {
            var sourceLanguage = LocalSettingsStore.GetSourceLanguage();
            var targetLanguage = LocalSettingsStore.GetTargetLanguage();

            // Startup must remain resilient and quick. Avoid network work here.
            var (sourceLanguageChoices, targetLanguageChoices) = DeepLTranslator.GetCachedSupportedLanguageChoices();
            EnsureChoicePresent(sourceLanguageChoices, sourceLanguage);
            EnsureChoicePresent(targetLanguageChoices, targetLanguage);

            var hasLanguageChoices = sourceLanguageChoices.Count > 1 && targetLanguageChoices.Count > 0;
            if (hasLanguageChoices)
            {
                var sourceLanguageSetting = new ChoiceSetSetting(
                    SourceLanguageSettingKey,
                    "Input language",
                    "Language you are translating from.",
                    sourceLanguageChoices)
                {
                    Value = sourceLanguage
                };

                var targetLanguageSetting = new ChoiceSetSetting(
                    TargetLanguageSettingKey,
                    "Target language",
                    "Language to translate into.",
                    targetLanguageChoices)
                {
                    Value = targetLanguage
                };

                settings.Add(sourceLanguageSetting);
                settings.Add(targetLanguageSetting);
                settings.SettingsChanged += (_, updatedSettings) =>
                {
                    if (updatedSettings.TryGetSetting<string>(SourceLanguageSettingKey, out var savedSourceLanguage))
                    {
                        settingsStore.SaveSourceLanguage(savedSourceLanguage);
                    }

                    if (updatedSettings.TryGetSetting<string>(TargetLanguageSettingKey, out var savedTargetLanguage))
                    {
                        settingsStore.SaveTargetLanguage(savedTargetLanguage);
                    }
                };
            }
        }
        catch (Exception ex)
        {
            StartupLog.Error("PowerTranslate: failed to initialize settings.", ex);
        }

        Settings = settings;
        DisplayName = "Power Translate";

        try
        {
            Icon = IconHelpers.FromRelativePath("Assets\\PowerTranslateLogo.png");
        }
        catch (Exception ex)
        {
            StartupLog.Error("PowerTranslate: failed to load icon.", ex);
        }

        var commands = new List<ICommandItem>();
        TryAddCommand(commands, () => new CommandItem(new DeepLSettingsPage())
        {
            Title = "Configure DeepL API key",
            Subtitle = "Password-masked input with validation"
        });
        TryAddCommand(commands, () => new CommandItem(new PowerTranslateExtensionPage())
        {
            Title = "Translate text",
            Subtitle = "Translate with configured source and target languages"
        });

        if (commands.Count == 0)
        {
            commands.Add(new CommandItem(new StartupUnavailableCommand())
            {
                Title = "PowerTranslate unavailable",
                Subtitle = "Startup failed. Check %LocalAppData%\\PowerTranslateExtension\\startup.log"
            });
            StartupLog.Error("PowerTranslate: no commands available after startup. Added fallback command.");
        }

        _commands = [.. commands];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    private static void EnsureChoicePresent(List<ChoiceSetSetting.Choice> choices, string selectedValue)
    {
        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            return;
        }

        if (choices.Any(choice => string.Equals(choice.Value, selectedValue, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var displayTitle = selectedValue switch
        {
            "AUTO" => "Auto",
            "EN" => "English",
            _ => selectedValue,
        };

        choices.Insert(0, new ChoiceSetSetting.Choice(displayTitle, selectedValue));
    }

    private static void TryAddCommand(List<ICommandItem> commands, Func<ICommandItem> createCommand)
    {
        try
        {
            commands.Add(createCommand());
        }
        catch (Exception ex)
        {
            StartupLog.Error("PowerTranslate: failed to create command.", ex);
        }
    }

    private sealed partial class StartupUnavailableCommand : InvokableCommand
    {
        public StartupUnavailableCommand()
        {
            Name = "Startup diagnostics";
        }

        public override ICommandResult Invoke()
        {
            return CommandResult.ShowToast(new ToastArgs
            {
                Message = "PowerTranslate startup failed. See startup.log in %LocalAppData%\\PowerTranslateExtension.",
                Result = CommandResult.KeepOpen(),
            });
        }
    }

}
