// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;

namespace MartinCostello.Benchmarks;

public sealed class DashboardFixture : IDisposable
{
    private bool _disposed;
    private string? _serverAddress;
    private Process? _server;

    ~DashboardFixture() => Dispose(false);

    public string ServerAddress => EnsureServer();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static string GetApplicationDirectory()
    {
        var solutionPath = typeof(DashboardFixture).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where((p) => p.Key == "SolutionPath")
            .Select((p) => p.Value)
            .Single();

        return Path.GetFullPath(Path.Combine(solutionPath!, "src", "Dashboard"));
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _server is not null)
            {
                try
                {
                    _server.Kill();
                }
                catch (Exception)
                {
                    // Ignore
                }

                _server.Dispose();
                _server = null;
            }

            _disposed = true;
        }
    }

    private string EnsureServer()
    {
        if (_serverAddress is null)
        {
            var path = GetApplicationDirectory();
            var timeout = TimeSpan.FromSeconds(45);

            (_server, _serverAddress) = AppLauncher.Launch(path, timeout);
        }

        return _serverAddress;
    }
}
