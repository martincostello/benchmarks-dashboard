// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks;

public abstract class UITest(ITestOutputHelper outputHelper) : IAsyncLifetime, IDisposable
{
    ~UITest()
    {
        Dispose(false);
    }

    protected abstract Uri ServerAddress { get; }

    protected ITestOutputHelper Output { get; } = outputHelper;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ValueTask InitializeAsync()
    {
        InstallPlaywright();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        // No-op
    }

    private static void InstallPlaywright()
    {
        int exitCode = Microsoft.Playwright.Program.Main(["install"]);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright exited with code {exitCode}");
        }
    }
}
