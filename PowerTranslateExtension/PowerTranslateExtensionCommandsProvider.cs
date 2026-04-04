// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslateExtension.Services;
using System;
using System.Collections.Generic;

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
        var languageChoices = new List<ChoiceSetSetting.Choice>
        {
            new("AUTO", "Auto"),
            new("EN", "English"),
            new("DE", "German"),
            new("VN", "Vietnamese")
        };

        var sourceLanguageSetting = new ChoiceSetSetting(
            SourceLanguageSettingKey,
            "Input language",
            "Language you are translating from.",
            languageChoices)
        {
            Value = settingsStore.GetSourceLanguage()
        };

        var targetLanguageSetting = new ChoiceSetSetting(
            TargetLanguageSettingKey,
            "Target language",
            "Language to translate into.",
            languageChoices)
        {
            Value = settingsStore.GetTargetLanguage()
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

        Settings = settings;

        DisplayName = "Power Translate";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
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
