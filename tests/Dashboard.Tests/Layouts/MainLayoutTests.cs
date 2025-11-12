// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;
using MartinCostello.Benchmarks.Layout;

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
}
