using System;
using System.Diagnostics;
using System.IO;

namespace PowerTranslateExtension;

internal static class StartupLog
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PowerTranslateExtension",
        "startup.log");

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
            // Never crash startup due to logging failures.
        }
    }
}
