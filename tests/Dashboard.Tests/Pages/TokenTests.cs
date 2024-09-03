// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using AngleSharp.Html.Dom;
using Bunit;
using JustEat.HttpClientInterception;
using MartinCostello.Benchmarks.Models;

namespace MartinCostello.Benchmarks.Pages;

public class TokenTests : DashboardTestContext
{
    [Fact]
    public void Page_Renders()
    {
        // Arrange
        var deviceCode = new GitHubDeviceCode()
        {
            DeviceCode = "3584d83530557fdd1f46af8289938c8ef79f9dc5",
            ExpiresInSeconds = 900,
            RefreshIntervalInSeconds = 5,
            UserCode = "WDJB-MJHT",
            VerificationUrl = "https://github.local/login/device",
        };

        WithDeviceCode(deviceCode);

        JSInterop.SetupVoid("configureClipboard", (_) => true);

        // Act
        var actual = RenderComponent<Token>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                var userCode = actual.Find("[id='user-code']");

                var input = userCode as IHtmlInputElement;
                input.ShouldNotBeNull();
                input.Value.ShouldBe(deviceCode.UserCode);

                actual.Find("[id='authorize']").TextContent.ShouldContain("Authorize");
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Page_Renders_If_Token_Cannot_Be_Generated()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl("https://github.local/login/device/code?client_id=dkd73mfo9ASgjsfnhJD8&scope=public_repo")
            .WithStatus(404);

        builder.RegisterWith(Interceptor);

        JSInterop.SetupVoid("configureClipboard", (_) => true);

        // Act
        var actual = RenderComponent<Token>();

        // Assert
        actual.WaitForAssertion(
            () => actual.Find("[id='generation-failed']"),
            TimeSpan.FromSeconds(2));
    }
}
