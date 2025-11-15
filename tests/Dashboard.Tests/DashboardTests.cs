// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MartinCostello.Benchmarks.PageModels;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;

namespace MartinCostello.Benchmarks;

[Collection(DashboardCollection.Name)]
public class DashboardTests(
    DashboardFixture fixture,
    ITestOutputHelper outputHelper) : UITest(outputHelper)
{
    private const string ValidFakeToken = "VALID_GITHUB_ACCESS_TOKEN";

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
            "alexa-london-travel",
            "alexa-london-travel-site",
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

        var browser = new BrowserFixture(options, Output);
        await browser.WithPageAsync(async page =>
        {
            await page.GotoAsync(fixture.ServerAddress);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            var cancelled = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var authorized = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            await ConfigureMocksAsync(page, cancelled, authorized);

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

            // Act
            await dashboard.ToggleThemeAsync();

            // Assert - Dark theme works
            await VerifyScreenshot(chart, $"{browserType}_{browserChannel}_benchmarks-demo_dark");

            // Act
            await dashboard.ToggleThemeAsync();

            // Assert - Back to light theme
            await VerifyScreenshot(chart, $"{browserType}_{browserChannel}_benchmarks-demo");

            // Arrange
            var token = await dashboard.SignInAsync();
            await token.WaitForContentAsync();

            // Act
            var firstCode = await token.UserCode();

            // Assert
            firstCode.ShouldNotBeNullOrWhiteSpace();

            // Act
            await token.Authorize();
            cancelled.SetResult(firstCode);

            // Assert
            await token.AuthorizationFailed().ShouldBeTrue();

            // Arrange
            await token.RefreshUserCode();
            await token.WaitForContentAsync();

            // Act
            var secondCode = await token.UserCode();

            // Assert
            secondCode.ShouldNotBeNullOrWhiteSpace();
            secondCode.ShouldNotBe(firstCode);

            // Act
            await token.Authorize();
            authorized.SetResult(secondCode);

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

            await VerifyScreenshot(chart, $"{browserType}_{browserChannel}_website");

            // Act
            await dashboard.SignOutAsync();

            // Assert
            await dashboard.WaitForSignedOutAsync();
        });
    }

    private static string JsonResponseFile(string name)
        => Path.Combine(".", "Responses", $"{name}.json");

    private static async Task VerifyScreenshot(IElementHandle element, string parametersText)
    {
        var screenshot = await element.ScreenshotAsync(new()
        {
            Quality = 50,
            Type = ScreenshotType.Jpeg,
        });

        using var stream = new MemoryStream(screenshot);
        await Verify(new Target("png", stream))
            .UseDirectory("snapshots")
            .UseTextForParameters(parametersText);
    }

    private static async Task ConfigureMocksAsync(
        IPage page,
        TaskCompletionSource<string> cancelled,
        TaskCompletionSource<string> authorized)
    {
        const string GitHubApi = "https://api.github.com";
        const string GitHubData = "https://raw.githubusercontent.com";
        const string GitHubLogin = "https://github.com/login/device";
        const string GitHubToken = "https://api.martincostello.com/github";
        const string Owner = "martincostello";

        page.Popup += async (_, popup) =>
        {
            await popup.WaitForLoadStateAsync();

            var uri = new Uri(popup.Url);

            uri.Host.ShouldBe("github.com");
            uri.Scheme.ShouldBe(Uri.UriSchemeHttps);
            uri.PathAndQuery.ShouldStartWith("/login");

            var query = QueryHelpers.ParseQuery(uri.Query);
            query.ShouldContainKeyAndValue("return_to", GitHubLogin);

            await popup.CloseAsync();
        };

        await ConfigureUserAsync(page, cancelled, authorized);
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

        static async Task ConfigureUserAsync(
            IPage page,
            TaskCompletionSource<string> cancelled,
            TaskCompletionSource<string> authorized)
        {
            string clientId = "Ov23likdXQFqdqFST1Ec";
            string scopes = "public_repo";

            string currentDeviceCode = GenerateDeviceCode();
            string currentUserCode = GenerateUserCode();

            object NewDeviceCode()
            {
                string newDeviceCode = GenerateDeviceCode();
                string newUserCode = GenerateUserCode();

                currentDeviceCode = newDeviceCode;
                currentUserCode = newUserCode;

                return new
                {
                    device_code = newDeviceCode,
                    user_code = newUserCode,
                    verification_uri = GitHubLogin,
                    expires_in = 899,
                    interval = 1,
                };
            }

            await page.RouteAsync($"{GitHubToken}/login/device/code?client_id={clientId}&scope={scopes}", async (route) =>
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    Json = NewDeviceCode(),
                });
            });

            await page.RouteAsync($"{GitHubToken}/login/oauth/access_token?client_id={clientId}&*", async (route) =>
            {
                var url = new Uri(route.Request.Url);
                var query = QueryHelpers.ParseQuery(url.Query);

                string response;

                if (query["device_code"] == currentDeviceCode)
                {
                    if (authorized.Task.IsCompleted && await authorized.Task == currentUserCode)
                    {
                        response = "authorized";
                    }
                    else if (cancelled.Task.IsCompleted && await cancelled.Task == currentUserCode)
                    {
                        response = "expired";
                    }
                    else
                    {
                        response = "pending";
                    }
                }
                else
                {
                    response = "incorrect_device_code";
                }

                await route.FulfillAsync(new()
                {
                    Status = 200,
                    Path = JsonResponseFile($"access-token-{response}"),
                });
            });

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

            static string GenerateDeviceCode()
                => Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.Ordinal);

            static string GenerateUserCode()
                => Guid.NewGuid().ToString().Substring(9, 9);
        }
    }
}
