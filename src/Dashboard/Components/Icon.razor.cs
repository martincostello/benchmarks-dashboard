// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;

namespace MartinCostello.Benchmarks.Components;

public partial class Icon
{
    /// <summary>
    /// Gets the name to use from the <see cref="Icons"/> class.
    /// </summary>
    [Parameter]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets any additional classes to add to the icon.
    /// </summary>
    [Parameter]
    public IList<string> Classes { get; set; } = [];

    /// <summary>
    /// Gets the optional color to use.
    /// </summary>
    [Parameter]
    public string? Color { get; init; }

    /// <summary>
    /// Gets a value indicating whether the icon is fixed-width.
    /// </summary>
    [Parameter]
    public bool FixedWidth { get; init; }

    /// <summary>
    /// Gets a value indicating whether the icon should spin.
    /// </summary>
    [Parameter]
    public bool Spin { get; init; }

    /// <summary>
    /// Gets a value indicating whether the icon is stacked.
    /// </summary>
    [Parameter]
    public bool Stacked { get; init; }

    /// <inheritdoc/>
    protected override bool ShouldRender() => false;

    private string ClassList()
    {
        List<string> classes = [Name];

        if (FixedWidth)
        {
            classes.Add("fa-fw");
        }

        if (Stacked)
        {
            classes.Add("fa-stack-1x");
        }

        if (Color is { Length: > 0 })
        {
            classes.Add($"text-{Color}");
        }

        if (Spin)
        {
            classes.Add("fa-spin");
        }

        if (Classes.Count > 0)
        {
            classes.AddRange(Classes);
        }

        return string.Join(' ', classes);
    }
}
