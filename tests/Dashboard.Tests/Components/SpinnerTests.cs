// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;

namespace MartinCostello.Benchmarks.Components;

public class SpinnerTests : DashboardTestContext
{
    [Theory]
    [InlineData("my-id", null, false, null, SpinnerType.Growing, "spinner spinner-grow spinner-grow-sm", "Loading...")]
    [InlineData("my-id", null, false, null, SpinnerType.Border, "spinner spinner-border spinner-border-sm", "Loading...")]
    [InlineData("my-id", "text-danger", false, null, SpinnerType.Growing, "spinner spinner-grow spinner-grow-sm text-danger", "Loading...")]
    [InlineData("my-id", null, true, null, SpinnerType.Growing, "spinner spinner-grow spinner-xl", "Loading...")]
    [InlineData("my-id", null, false, "Please wait...", SpinnerType.Growing, "spinner spinner-grow spinner-grow-sm", "Please wait...")]
    public void Spinner_Renders_Correctly(
        string id,
        string? color,
        bool large,
        string? loadingText,
        SpinnerType spinnerType,
        string expectedClasses,
        string expectedTitle)
    {
        // Act
        var actual = Render<Spinner>((builder) =>
        {
            builder.Add((p) => p.Id, id)
                   .Add((p) => p.Color, color)
                   .Add((p) => p.Large, large)
                   .Add((p) => p.SpinnerType, spinnerType);

            if (loadingText is not null)
            {
                builder.Add((p) => p.LoadingText, loadingText);
            }
        });

        // Assert
        var element = actual.Find($"[id={id}]");

        element.GetAttribute("class").ShouldBe(expectedClasses);
        element.GetAttribute("role").ShouldBe("status");
        element.GetAttribute("title").ShouldBe(expectedTitle);
        element.TextContent.ShouldBe(expectedTitle);
    }
}
