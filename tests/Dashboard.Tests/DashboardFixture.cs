// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;

namespace MartinCostello.Benchmarks;

public sealed class DashboardFixture : IAsyncLifetime
{
    private string? _serverAddress;
    private Process? _server;

    public string ServerAddress => _serverAddress ?? throw new InvalidOperationException("Server is not running.");

    public async ValueTask InitializeAsync()
    {
        if (_serverAddress is null)
        {
            var path = GetApplicationDirectory();
            var timeout = TimeSpan.FromSeconds(45);

            (_server, _serverAddress) = await AppLauncher.LaunchAsync(path, timeout);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_server is not null)
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

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
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
}
