// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace MartinCostello.Benchmarks;

/// <summary>
/// Launches a Blazor WASM application.
/// </summary>
/// <remarks>
/// Adapted from https://github.com/dotnet/aspnetcore/blob/63c492e22b06d4903cb4e7aee037295c71e1ec37/src/Components/WebAssembly/Server/src/DebugProxyLauncher.cs.
/// </remarks>
internal static class AppLauncher
{
    public static (Process Process, string Url) Launch(string path, TimeSpan timeout)
    {
#if DEBUG
        string configuration = "Debug";
#else
        string configuration = "Release";
#endif

        // See https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints#configure-endpoints
        int port = GetFreePort();
        var serverAddress = FormattableString.Invariant($"https://localhost:{port}");

        var startInfo = new ProcessStartInfo("dotnet", ["run", "--configuration", configuration, "--", "--urls", serverAddress])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = path,
        };

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var tokenSource = new CancellationTokenSource(timeout);

        var server = Process.Start(startInfo);

        WaitForStart(server, completionSource);

        using var registration = tokenSource.Token.Register(
            () => completionSource.TrySetException(
                new TimeoutException($"Failed to start the dashboard within {timeout.TotalSeconds} seconds.")));

        completionSource.Task.Wait();

        return (server!, serverAddress);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void WaitForStart(Process? process, TaskCompletionSource completionSource)
    {
        if (process is null)
        {
            completionSource.TrySetException(new InvalidOperationException("Unable to start dashboard."));
            return;
        }

        var errorEncountered = false;

        process.ErrorDataReceived += OnErrorDataReceived;
        process.BeginErrorReadLine();

        process.OutputDataReceived += OnOutputDataReceived;
        process.BeginOutputReadLine();

        void OnErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data is { Length: > 0 } data)
            {
                _ = completionSource.TrySetException(new InvalidOperationException(data));
                errorEncountered = true;
            }
        }

        void OnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data))
            {
                if (!errorEncountered)
                {
                    _ = completionSource.TrySetException(new InvalidOperationException("Expected output has not been received from the application."));
                }
            }
            else if (Regex.IsMatch(eventArgs.Data, @"^\s*Application started\. Press Ctrl\+C to shut down\.$", RegexOptions.None, TimeSpan.FromSeconds(10)))
            {
                process.OutputDataReceived -= OnOutputDataReceived;
                process.ErrorDataReceived -= OnErrorDataReceived;

                _ = completionSource.TrySetResult();
            }
        }
    }
}
