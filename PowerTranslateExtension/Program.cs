// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace PowerTranslateExtension;

public class Program
{
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(nint dpiFlag);

    private static readonly nint DpiAwarenessContextPerMonitorAwareV2 = new(-4);

    [MTAThread]
    public static void Main(string[] args)
    {
        TryEnablePerMonitorV2DpiAwareness();

        try
        {
            if (IsComServerActivation(args))
            {
                StartupLog.Info("Starting COM server activation.");

                global::Shmuelie.WinRTServer.ComServer server = new();
                ManualResetEvent extensionDisposedEvent = new(false);

                try
                {
                    // We are instantiating an extension instance once above, and returning it every time the callback in RegisterExtension below is called.
                    // This makes sure that only one instance of SampleExtension is alive, which is returned every time the host asks for the IExtension object.
                    // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
                    PowerTranslateExtension extensionInstance = new(extensionDisposedEvent);
                    server.RegisterClass<PowerTranslateExtension, IExtension>(() => extensionInstance);
                    server.Start();
                    StartupLog.Info("COM server started successfully.");

                    // This will make the main thread wait until the event is signalled by the extension class.
                    // Since we have single instance of the extension object, we exit as soon as it is disposed.
                    extensionDisposedEvent.WaitOne();
                    server.Stop();
                    server.UnsafeDispose();
                    StartupLog.Info("COM server shut down.");
                }
                catch (Exception ex)
                {
                    StartupLog.Error("COM server activation failed.", ex);
                    return;
                }
            }
            else
            {
                StartupLog.Info($"Not launched as extension COM server. Exiting. Args: {string.Join(' ', args)}");
                Console.WriteLine("Not being launched as a Extension... exiting.");
            }
        }
        catch (Exception ex)
        {
            StartupLog.Error("Unhandled startup exception.", ex);
            return;
        }
    }

    private static void TryEnablePerMonitorV2DpiAwareness()
    {
        try
        {
            _ = SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
        }
        catch
        {
            // Never fail extension startup due to DPI API availability.
        }
    }

    private static bool IsComServerActivation(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        return args.Any(static arg =>
            string.Equals(arg, "-RegisterProcessAsComServer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "/RegisterProcessAsComServer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "-Embedding", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "/Embedding", StringComparison.OrdinalIgnoreCase));
    }
}
