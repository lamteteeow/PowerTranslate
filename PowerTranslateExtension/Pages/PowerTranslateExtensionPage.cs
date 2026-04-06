// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslateExtension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerTranslateExtension;

internal sealed partial class PowerTranslateExtensionPage : DynamicListPage
{
    private readonly LocalSettingsStore _settingsStore = new();
    private readonly DeepLTranslator _translator;
    private readonly object _debounceLock = new();
    private CancellationTokenSource? _debounceCts;
    private string _searchText = string.Empty;
    private string _resultTitle = string.Empty;
    private string _resultBody = "Type or paste text in the search box. Translation happens automatically after you pause typing.\n\nTip: First configure your DeepL API key, then reload Command Palette extensions.";
    private int _translationGeneration;

    public PowerTranslateExtensionPage()
    {
        _translator = new DeepLTranslator(_settingsStore);
        Icon = IconHelpers.FromRelativePath("Assets\\PowerTranslateLogo.png");
        Title = "PowerTranslate";
        PlaceholderText = "Type or paste text to translate";
        ShowDetails = true;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _searchText = newSearch ?? string.Empty;
        ScheduleTranslation(_searchText);
    }

    public override IListItem[] GetItems()
    {
        var languageChoices = _translator.GetSupportedLanguageChoices();
        var sourceLanguage = LocalSettingsStore.GetSourceLanguage();
        var targetLanguage = LocalSettingsStore.GetTargetLanguage();

        var items = new List<IListItem>
        {
            new ListItem(new CopyTextCommand(_resultBody)
            {
                Name = "Copy result",
                Result = CommandResult.ShowToast(new ToastArgs
                {
                    Message = "Copied to clipboard",
                    Result = CommandResult.KeepOpen(),
                })
            })
            {
                Title = "Copy result",
                Subtitle = string.Empty,
                Details = BuildResultDetails(),
            }
        };

        if (languageChoices.TargetChoices.Count > 0)
        {
            items.Add(new ListItem(CreateTargetLanguagePage(languageChoices.TargetChoices, targetLanguage))
            {
                Title = "Change target language",
                Subtitle = GetLanguageDisplayName(languageChoices.TargetChoices, targetLanguage),
                Details = BuildResultDetails(),
            });
        }

        if (languageChoices.SourceChoices.Count > 0)
        {
            items.Add(new ListItem(CreateSourceLanguagePage(languageChoices.SourceChoices, sourceLanguage))
            {
                Title = "Change source language",
                Subtitle = GetLanguageDisplayName(languageChoices.SourceChoices, sourceLanguage),
                Details = BuildResultDetails(),
            });
        }

        return [.. items];
    }

    private void ScheduleTranslation(string input)
    {
        StartTranslation(input, TimeSpan.FromSeconds(1));
    }

    private void RefreshTranslation()
    {
        StartTranslation(_searchText, TimeSpan.Zero);
    }

    private void StartTranslation(string input, TimeSpan delay)
    {
        var trimmedInput = (input ?? string.Empty).Trim();
        var generation = Interlocked.Increment(ref _translationGeneration);

        CancellationTokenSource? previousCts;
        var currentCts = new CancellationTokenSource();
        lock (_debounceLock)
        {
            previousCts = _debounceCts;
            _debounceCts = currentCts;
        }

        previousCts?.Cancel();
        previousCts?.Dispose();

        if (string.IsNullOrWhiteSpace(trimmedInput))
        {
            _resultTitle = string.Empty;
            _resultBody = "Type or paste text in the search box. Translation happens automatically after you pause typing.\n\nTip: First configure your DeepL API key, then reload Command Palette extensions.";
            RaiseItemsChanged(1);
            return;
        }

        _ = TranslateAfterDelayAsync(trimmedInput, generation, delay, currentCts.Token);
    }

    private async Task TranslateAfterDelayAsync(string input, int generation, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            if (generation != _translationGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _resultTitle = string.Empty;
            _resultBody = "Translating...";
            RaiseItemsChanged(1);

            var sourceLanguage = LocalSettingsStore.GetSourceLanguage();
            var targetLanguage = LocalSettingsStore.GetTargetLanguage();
            var result = await Task.Run(() => _translator.Translate(input, sourceLanguage, targetLanguage), cancellationToken)
                .ConfigureAwait(false);

            if (generation != _translationGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _resultTitle = string.Empty;
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.SourceLanguage) && !string.IsNullOrWhiteSpace(result.TargetLanguage))
            {
                _resultBody = $"{result.SourceLanguage} -> {result.TargetLanguage}\n{result.Message}";
            }
            else
            {
                _resultBody = result.Message;
            }
            RaiseItemsChanged(1);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (generation != _translationGeneration)
            {
                return;
            }

            _resultTitle = string.Empty;
            _resultBody = ex.Message;
            RaiseItemsChanged(1);
        }
    }

    private Details BuildResultDetails()
    {
        return new Details
        {
            Title = _resultTitle,
            Body = EscapeMarkdown(_resultBody),
            Size = ContentSize.Medium
        };
    }

    private static string EscapeMarkdown(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\r\n", "\n").Replace("\n", "  \n").Replace("`", "\\`");
    }

    private static string GetLanguageDisplayName(IEnumerable<ChoiceSetSetting.Choice> choices, string selectedCode)
    {
        var selected = choices.FirstOrDefault(choice =>
            string.Equals(choice.Value, selectedCode, StringComparison.OrdinalIgnoreCase));

        if (selected is null || string.IsNullOrWhiteSpace(selected.Title))
        {
            return selectedCode;
        }

        if (string.Equals(selected.Title, selectedCode, StringComparison.OrdinalIgnoreCase))
        {
            return selected.Title;
        }

        if (string.Equals(selectedCode, "AUTO", StringComparison.OrdinalIgnoreCase))
        {
            return selected.Title;
        }

        return $"{selected.Title} ({selectedCode})";
    }

    private LanguageSelectionPage CreateSourceLanguagePage(IEnumerable<ChoiceSetSetting.Choice> choices, string selectedLanguage)
    {
        return new LanguageSelectionPage(
            "Change source language",
            choices,
            selectedLanguage,
            isSourceLanguage: true,
            _settingsStore.SaveSourceLanguage,
            RefreshTranslation);
    }

    private LanguageSelectionPage CreateTargetLanguagePage(IEnumerable<ChoiceSetSetting.Choice> choices, string selectedLanguage)
    {
        return new LanguageSelectionPage(
            "Change target language",
            choices,
            selectedLanguage,
            isSourceLanguage: false,
            _settingsStore.SaveTargetLanguage,
            RefreshTranslation);
    }

    private sealed partial class LanguageSelectionPage : ListPage
    {
        private readonly List<ChoiceSetSetting.Choice> _choices;
        private readonly string _selectedLanguage;
        private readonly bool _isSourceLanguage;
        private readonly Action<string?> _saveLanguage;
        private readonly Action _refreshTranslation;

        public LanguageSelectionPage(
            string title,
            IEnumerable<ChoiceSetSetting.Choice> choices,
            string selectedLanguage,
            bool isSourceLanguage,
            Action<string?> saveLanguage,
            Action refreshTranslation)
        {
            _choices = choices.ToList();
            _selectedLanguage = selectedLanguage;
            _isSourceLanguage = isSourceLanguage;
            _saveLanguage = saveLanguage;
            _refreshTranslation = refreshTranslation;

            Icon = IconHelpers.FromRelativePath("Assets\\PowerTranslateLogo.png");
            Title = title;
            Name = "Open";
            ShowDetails = false;
        }

        public override IListItem[] GetItems()
        {
            var items = new List<IListItem>();

            if (_isSourceLanguage)
            {
                items.Add(CreateChoiceItem("Auto", "AUTO"));
            }

            foreach (var choice in _choices)
            {
                if (_isSourceLanguage && string.Equals(choice.Value, "AUTO", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                items.Add(CreateChoiceItem(choice.Title, choice.Value));
            }

            return [.. items];
        }

        private ListItem CreateChoiceItem(string title, string value)
        {
            var isSelected = string.Equals(value, _selectedLanguage, StringComparison.OrdinalIgnoreCase);
            return new ListItem(new SelectLanguageCommand(value, _saveLanguage, _refreshTranslation))
            {
                Title = title,
                Subtitle = isSelected ? "Current" : value,
            };
        }

        private sealed partial class SelectLanguageCommand(string value, Action<string?> saveLanguage, Action refreshTranslation) : InvokableCommand
        {
            public override string Name { get; set; } = "Select language";

            public override CommandResult Invoke()
            {
                saveLanguage(value);
                refreshTranslation();
                return CommandResult.GoBack();
            }
        }
    }
}