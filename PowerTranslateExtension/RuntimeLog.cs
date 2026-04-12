using System;
using System.Diagnostics;
using System.IO;
using PowerTranslateExtension.Services;

namespace PowerTranslateExtension;

internal static class RuntimeLog
{
    private static readonly object StateLock = new();
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PowerTranslateExtension",
        "runtime.log");

    private static bool _enabled = LocalSettingsStore.GetRuntimeLoggingEnabled();

    public static bool IsEnabled
    {
        get
        {
            lock (StateLock)
            {
                return _enabled;
            }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        var becameEnabled = false;

        lock (StateLock)
        {
            becameEnabled = enabled && !_enabled;
            _enabled = enabled;
        }

        LocalSettingsStore.SaveRuntimeLoggingEnabled(enabled);

        if (becameEnabled)
        {
            Write("INFO", "Runtime logging enabled.");
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        if (ex is null)
        {
            Write("ERROR", message);
        }
        else
        {
            Write("ERROR", message + Environment.NewLine + ex);
        }
    }

    private static void Write(string level, string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        var line = $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}";
        Debug.WriteLine(line);

        try
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(LogFilePath, line);
        }
        catch
        {
            // Never break runtime behavior because of log writes.
        }
    }
}
