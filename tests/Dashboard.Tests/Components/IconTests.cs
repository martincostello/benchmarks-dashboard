// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;

namespace MartinCostello.Benchmarks.Components;

public class IconTests : DashboardTestContext
{
    [Theory]
    [InlineData("fa-brands fa-github", null, false, false, false, "fa-brands fa-github")]
    [InlineData("fa-brands fa-github", "dark", false, false, false, "fa-brands fa-github text-dark")]
    [InlineData("fa-brands fa-github", null, true, false, false, "fa-brands fa-github fa-fw")]
    [InlineData("fa-brands fa-github", null, false, true, false, "fa-brands fa-github fa-spin")]
    [InlineData("fa-brands fa-github", null, false, false, true, "fa-brands fa-github fa-stack-1x")]
    [InlineData("fa-brands fa-github", "danger", true, true, true, "fa-brands fa-github text-danger fa-fw fa-spin fa-stack-1x")]
    public void Icon_Renders_Correctly(
        string name,
        string? color,
        bool fixedWidth,
        bool spin,
        bool stacked,
        string expected)
    {
        // Act
        var actual = Render<Icon>((builder) =>
        {
            builder.Add((p) => p.Name, name)
                   .Add((p) => p.Color, color)
                   .Add((p) => p.FixedWidth, fixedWidth)
                   .Add((p) => p.Spin, spin)
                   .Add((p) => p.Stacked, stacked);
        });

        // Assert
        actual.MarkupMatches($@"<span class=""{expected}"" aria-hidden=""true""></span>");
    }
}
