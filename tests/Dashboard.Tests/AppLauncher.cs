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
    public static async Task<(Process Process, string Url)> LaunchAsync(string path, TimeSpan timeout)
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

        var server = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start dashboard.");

        var isStarted = false;
        var errors = new List<string>();
        var matchTimeout = TimeSpan.FromSeconds(10);

        await foreach (var line in server.ReadAllLinesAsync(tokenSource.Token).WithCancellation(tokenSource.Token))
        {
            if (line.StandardError)
            {
                errors.Add(line.Content);
                continue;
            }

            if (Regex.IsMatch(line.Content, @"^\s*Application started\. Press Ctrl\+C to shut down\.$", RegexOptions.None, matchTimeout))
            {
                isStarted = true;
                break;
            }
        }

        if (!isStarted)
        {
            throw new TimeoutException($"Failed to start the dashboard within {timeout.TotalSeconds} seconds.{string.Join(Environment.NewLine, errors)}");
        }

        return (server, serverAddress);
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
}
