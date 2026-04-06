// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerTranslateExtension.Services;
using System;
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
    private string _resultBody = "Type or paste text in the search box. Translation happens automatically after you pause typing.";
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
        return
        [
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
        ];
    }

    private void ScheduleTranslation(string input)
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
            _resultBody = "Type or paste text in the search box. Translation happens automatically after you pause typing.";
            RaiseItemsChanged(1);
            return;
        }

        _ = TranslateAfterDelayAsync(trimmedInput, generation, currentCts.Token);
    }

    private async Task TranslateAfterDelayAsync(string input, int generation, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            if (generation != _translationGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _resultTitle = string.Empty;
            _resultBody = "Translating...";
            RaiseItemsChanged(1);

            var sourceLanguage = _settingsStore.GetSourceLanguage();
            var targetLanguage = _settingsStore.GetTargetLanguage();
            var result = await Task.Run(() => _translator.Translate(input, sourceLanguage, targetLanguage), cancellationToken)
                .ConfigureAwait(false);

            if (generation != _translationGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _resultTitle = string.Empty;
            _resultBody = result.Message;
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
            Size = ContentSize.Large
        };
    }

    private static string EscapeMarkdown(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\r\n", "\n").Replace("\n", "  \n").Replace("`", "\\`");
    }
}