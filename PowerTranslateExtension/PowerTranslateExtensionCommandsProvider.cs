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
        var translator = new DeepLTranslator(settingsStore);
        var settings = new Settings();
        var sourceLanguage = settingsStore.GetSourceLanguage();
        var targetLanguage = settingsStore.GetTargetLanguage();
        var (sourceLanguageChoices, targetLanguageChoices) = translator.GetSupportedLanguageChoices();

        var hasLanguageChoices = sourceLanguageChoices.Count > 0 && targetLanguageChoices.Count > 0;

        if (hasLanguageChoices && !sourceLanguageChoices.Any(c => string.Equals(c.Value, sourceLanguage, StringComparison.Ordinal)))
        {
            sourceLanguage = sourceLanguageChoices[0].Value;
        }

        if (hasLanguageChoices && !targetLanguageChoices.Any(c => string.Equals(c.Value, targetLanguage, StringComparison.Ordinal)))
        {
            targetLanguage = targetLanguageChoices[0].Value;
        }

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
                if (updatedSettings.TryGetSetting<string>(SourceLanguageSettingKey, out var sourceLanguage))
                {
                    settingsStore.SaveSourceLanguage(sourceLanguage);
                }

                if (updatedSettings.TryGetSetting<string>(TargetLanguageSettingKey, out var targetLanguage))
                {
                    settingsStore.SaveTargetLanguage(targetLanguage);
                }
            };
        }

        Settings = settings;

        DisplayName = "Power Translate";
        Icon = IconHelpers.FromRelativePath("Assets\\PowerTranslateLogo.png");
        _commands = [
            new CommandItem(new DeepLSettingsPage())
            {
                Title = "Configure DeepL API key",
                Subtitle = "Password-masked input with validation"
            },
            new CommandItem(new PowerTranslateExtensionPage())
            {
                Title = "Translate text",
                Subtitle = "Translate with configured source and target languages"
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
