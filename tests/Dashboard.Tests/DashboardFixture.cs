// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Diagnostics;

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
        string contentRoot = string.Empty;
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(typeof(DashboardFixture).Assembly.Location)!);

        do
        {
            string? solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, "Dashboard.sln").FirstOrDefault();

            if (solutionPath is not null)
            {
                return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "src", "Dashboard"));
            }

            directoryInfo = directoryInfo.Parent;
        }
        while (directoryInfo is not null);

        return contentRoot;
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
            var timeout = TimeSpan.FromSeconds(30);

            (_server, _serverAddress) = AppLauncher.Launch(path, timeout);
        }

        return _serverAddress;
    }
}
