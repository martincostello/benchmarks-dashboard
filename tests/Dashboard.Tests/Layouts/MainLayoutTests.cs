// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;
using MartinCostello.Benchmarks.Layout;
using Microsoft.AspNetCore.Components.Web;

namespace MartinCostello.Benchmarks.Layouts;

public class MainLayoutTests : DashboardTestContext
{
    [Fact]
    public void Component_Renders()
    {
        // Arrange
        JSInterop.SetupVoid("configureToolTips");

        // Act
        var actual = Render<MainLayout>();

        // Assert
        actual.Find(".blazor");
    }

    [Fact]
    public void Component_Loads_Configured_Custom_Stylesheet()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        Options.CustomCssUrl = "my-custom.css";

        var head = Render<HeadOutlet>();

        // Act
        Render<MainLayout>();

        // Assert
        head.Markup.ShouldContain("href=\"my-custom.css\"");
    }
}
