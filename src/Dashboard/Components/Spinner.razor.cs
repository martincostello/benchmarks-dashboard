// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;

namespace MartinCostello.Benchmarks.Components;

public partial class Spinner
{
    /// <summary>
    /// Gets the optional color for the spinner.
    /// </summary>
    [Parameter]
    public string? Color { get; init; }

    /// <summary>
    /// Gets the optional ID for the spinner.
    /// </summary>
    [Parameter]
    public string? Id { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the spinner is large.
    /// </summary>
    [Parameter]
    public bool Large { get; set; }

    /// <summary>
    /// Gets the text to show while the spinner is loading.
    /// </summary>
    [Parameter]
    public string LoadingText { get; init; } = "Loading...";

    /// <summary>
    /// Gets the type of the spinner.
    /// </summary>
    [Parameter]
    public SpinnerType SpinnerType { get; init; }

    /// <inheritdoc/>
    protected override bool ShouldRender() => false;

    private string ClassList()
    {
        string spinnerClass = SpinnerType == SpinnerType.Growing ? "spinner-grow" : "spinner-border";
        List<string> classes = ["spinner", spinnerClass];

        if (Large)
        {
            classes.Add("spinner-xl");
        }
        else
        {
            classes.Add($"{spinnerClass}-sm");
        }

        if (Color is { Length: > 0 })
        {
            classes.Add(Color);
        }

        return string.Join(' ', classes);
    }
}
