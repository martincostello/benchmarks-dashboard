// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;
using MartinCostello.Benchmarks.Layout;

namespace MartinCostello.Benchmarks.Layouts;

public class NavbarTests : DashboardTestContext
{
    [Fact]
    public async Task Component_Renders_When_Signed_In()
    {
        // Arrange
        await WithValidAccessToken();

        // Act
        var actual = Render<Navbar>();

        // Assert
        actual.WaitForAssertion(
            () => actual.Find("[id='user-name']").TextContent.ShouldContain("speedy"),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Component_Renders_When_Signed_Out()
    {
        // Arrange
        UsingGitHubDotCom();

        // Act
        var actual = Render<Navbar>();

        // Assert
        actual.WaitForAssertion(
            () => actual.Find("[id='sign-in']").TextContent.ShouldContain("Sign in"),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Theme_Can_Be_Toggled()
    {
        // Arrange
        JSInterop.SetupVoid("toggleTheme");

        var actual = Render<Navbar>();

        // Act
        actual.Find("[id='toggle-theme']").Click();

        // Assert
        JSInterop.VerifyInvoke("toggleTheme");
    }
}
