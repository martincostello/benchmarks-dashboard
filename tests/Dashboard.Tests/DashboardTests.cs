// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MartinCostello.Benchmarks.Pages;
using Microsoft.Playwright;

namespace MartinCostello.Benchmarks;

[Collection(DashboardCollection.Name)]
public class DashboardTests(
    DashboardFixture fixture,
    ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private const string ValidFakeToken = "VALID_GITHUB_ACCESS_TOKEN";

    private DashboardFixture Fixture { get; } = fixture;

    private ITestOutputHelper OutputHelper { get; } = outputHelper;

    public static TheoryData<string, string?> Browsers()
    {
        var browsers = new TheoryData<string, string?>()
        {
            { BrowserType.Chromium, null },
            { BrowserType.Chromium, "chrome" },
            { BrowserType.Firefox, null },
        };

        return browsers;
    }

#pragma warning disable xUnit1013
    [ModuleInitializer]
    public static void InitPlaywright()
    {
        VerifyImageMagick.Initialize();
        VerifyImageMagick.RegisterComparers(threshold: 0.25);
        VerifyPlaywright.Initialize();
    }
#pragma warning restore xUnit1013

    [Theory]
    [MemberData(nameof(Browsers))]
    public async Task Can_View_Benchmarks(string browserType, string? browserChannel)
    {
        // Arrange
        string[] expectedRepos =
        [
            "benchmarks-demo",
            "adventofcode",
            "api",
            "aspnetcore-openapi",
            "costellobot",
            "openapi-extensions",
            "project-euler",
            "website",
        ];

        var options = new BrowserFixtureOptions
        {
            BrowserType = browserType,
            BrowserChannel = browserChannel,
        };

        var browser = new BrowserFixture(options, OutputHelper);
        await browser.WithPageAsync(async page =>
        {
            await page.GotoAsync(Fixture.ServerAddress);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await ConfigureMocksAsync(page);

            var dashboard = new HomePage(page);

            // Act and Assert
            await dashboard.WaitForContentAsync();
            await dashboard.Repository().ShouldBe("benchmarks-demo");
            await dashboard.Branch().ShouldBe("main");

            await dashboard.Repositories().ShouldBe(expectedRepos);
            await dashboard.Branches().ShouldBe(["main", "dotnet-nightly", "dotnet-vnext"]);

            var benchmarks = await dashboard.Benchmarks();

            benchmarks.ShouldContainKey("DotNetBenchmarks.IndexOfAnyBenchmarks");
            benchmarks["DotNetBenchmarks.IndexOfAnyBenchmarks"].ShouldBe(
            [
                "DotNetBenchmarks.IndexOfAnyBenchmarks.IndexOfAny_String",
                "DotNetBenchmarks.IndexOfAnyBenchmarks.IndexOfAny_Span_Array",
                "DotNetBenchmarks.IndexOfAnyBenchmarks.IndexOfAny_Span_Two_Chars",
            ]);

            benchmarks.ShouldContainKey("DotNetBenchmarks.HashBenchmarks");
            benchmarks["DotNetBenchmarks.HashBenchmarks"].ShouldBe(
            [
                "DotNetBenchmarks.HashBenchmarks.Sha256ComputeHash",
                "DotNetBenchmarks.HashBenchmarks.Sha256HashData",
            ]);

            benchmarks.ShouldContainKey("DotNetBenchmarks.ILoggerFactoryBenchmarks");
            benchmarks["DotNetBenchmarks.ILoggerFactoryBenchmarks"].ShouldBe(
            [
                "DotNetBenchmarks.ILoggerFactoryBenchmarks.CreateLogger_Generic",
                "DotNetBenchmarks.ILoggerFactoryBenchmarks.CreateLogger_Type",
            ]);

            benchmarks.ShouldContainKey("DotNetBenchmarks.TodoAppBenchmarks");
            benchmarks["DotNetBenchmarks.TodoAppBenchmarks"].ShouldBe(
            [
                "DotNetBenchmarks.TodoAppBenchmarks.GetAllTodos",
                "DotNetBenchmarks.TodoAppBenchmarks.GetOneTodo",
            ]);

            var chart = await dashboard.GetChart(
                "DotNetBenchmarks.TodoAppBenchmarks",
                "DotNetBenchmarks.TodoAppBenchmarks.GetAllTodos");

            await Verify(chart)
                .LocatorScreenshotOptions(new()
                {
                    Quality = 50,
                    Type = ScreenshotType.Jpeg,
                })
                .UseDirectory("snapshots")
                .UseTextForParameters($"{browserType}_{browserChannel}_benchmarks-demo");

            // Arrange
            var token = await dashboard.SignInAsync();
            await token.WaitForContentAsync();

            // Act
            await token.WithToken(Guid.NewGuid().ToString());
            await token.SaveToken();

            // Assert
            await token.TokenIsInvalid().ShouldBeTrue();

            // Act
            await token.WithToken(ValidFakeToken);
            await token.SaveToken();

            // Assert
            await dashboard.WaitForSignedInAsync();
            await dashboard.UserNameAsync().ShouldBe("speedy");
            await dashboard.Repository().ShouldBe("benchmarks-demo");
            await dashboard.Branch().ShouldBe("main");

            await dashboard.Repositories().ShouldBe(expectedRepos);
            await dashboard.Branches().ShouldBe(["main", "dotnet-nightly", "dotnet-vnext"]);

            // Act
            await dashboard.WithRepository("website");

            // Assert
            await dashboard.WaitForContentAsync();
            await dashboard.Repository().ShouldBe("website");
            await dashboard.Branch().ShouldBe("main");
            await dashboard.Repositories().ShouldBe(expectedRepos);
            await dashboard.Branches().ShouldBe(["main", "dev"]);

            benchmarks = await dashboard.Benchmarks();

            benchmarks.ShouldContainKey("Website");
            benchmarks["Website"].ShouldBe(
            [
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Root",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.About",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Projects",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Tools",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Version",
            ]);

            // Act
            await dashboard.WithBranch("dev");

            // Assert
            await dashboard.WaitForContentAsync();
            await dashboard.Repository().ShouldBe("website");
            await dashboard.Branch().ShouldBe("dev");
            await dashboard.Repositories().ShouldBe(expectedRepos);
            await dashboard.Branches().ShouldBe(["main", "dev"]);

            benchmarks = await dashboard.Benchmarks();

            benchmarks.ShouldContainKey("Website");
            benchmarks["Website"].ShouldBe(
            [
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Root",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.About",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Projects",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Tools",
            ]);

            chart = await dashboard.GetChart(
                "Website",
                "MartinCostello.Website.Benchmarks.WebsiteBenchmarks.Tools");

            chart.ShouldNotBeNull();

            await Verify(chart)
                .LocatorScreenshotOptions(new()
                {
                    Quality = 50,
                    Type = ScreenshotType.Jpeg,
                })
                .UseDirectory("snapshots")
                .UseTextForParameters($"{browserType}_{browserChannel}_website");

            // Act
            await dashboard.SignOutAsync();

            // Assert
            await dashboard.WaitForSignedOutAsync();
        });
    }

    public Task InitializeAsync()
    {
        InstallPlaywright();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string JsonResponseFile(string name)
        => Path.Combine(".", "Responses", $"{name}.json");

    private static void InstallPlaywright()
    {
        int exitCode = Microsoft.Playwright.Program.Main(["install"]);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright exited with code {exitCode}");
        }
    }

    private static async Task ConfigureMocksAsync(IPage page)
    {
        const string GitHubApi = "https://api.github.com";
        const string GitHubData = "https://raw.githubusercontent.com";
        const string Owner = "martincostello";

        await ConfigureUserAsync(page);
        await ConfigureRepoAsync(page, "benchmarks-demo", ["main"]);
        await ConfigureRepoAsync(page, "website", ["main", "dev"]);

        static async Task ConfigureRepoAsync(IPage page, string repo, string[] branches)
        {
            await page.RouteAsync($"{GitHubApi}/repos/{Owner}/{repo}", async (route) =>
            {
                await route.FulfillAsync(new()
                {
                    Path = JsonResponseFile($"{repo}-repo"),
                });
            });

            await page.RouteAsync($"{GitHubApi}/repos/{Owner}/{repo}/branches", async (route) =>
            {
                await route.FulfillAsync(new()
                {
                    Path = JsonResponseFile($"{repo}-branches"),
                });
            });

            foreach (var branch in branches)
            {
                await page.RouteAsync($"{GitHubData}/{Owner}/benchmarks/{branch}/{repo}/data.json", async (route) =>
                {
                    await route.FulfillAsync(new()
                    {
                        Path = JsonResponseFile($"{repo}-{branch}"),
                    });
                });
            }
        }

        static async Task ConfigureUserAsync(IPage page)
        {
            await page.RouteAsync($"{GitHubApi}/user", async (route) =>
            {
                const string Authorization = "authorization";

                route.Request.Headers.ShouldContainKey(Authorization);
                var token = route.Request.Headers[Authorization];

                if (token == $"token {ValidFakeToken}")
                {
                    await route.FulfillAsync(new()
                    {
                        Path = JsonResponseFile("user-valid-token"),
                        Status = 200,
                    });
                }
                else
                {
                    await route.FulfillAsync(new()
                    {
                        Path = JsonResponseFile("user-invalid-token"),
                        Status = 401,
                    });
                }
            });
        }
    }
}
