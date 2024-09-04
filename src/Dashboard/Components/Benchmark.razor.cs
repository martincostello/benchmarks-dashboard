// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks.Components;

public partial class Benchmark
{
    /// <summary>
    /// Gets the benchmark name.
    /// </summary>
    [Parameter]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the name of the associated benchmark suite.
    /// </summary>
    [Parameter]
    public required string Suite { get; init; }

    /// <summary>
    /// Gets the benchmark items.
    /// </summary>
    [Parameter]
    public required IList<BenchmarkItem> Items { get; init; }

    /// <summary>
    /// Gets the <see cref="IJSRuntime"/> to use.
    /// </summary>
    [Inject]
    public required IJSRuntime JS { get; init; }

    private string Id => $"{Suite}-{Name}";

    private string ChartId => $"{Suite}-{Name}-chart";

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var current = Options.Value;

        if (!current.DataSetColors.TryGetValue("Memory", out var memoryColor) ||
            memoryColor is not { Length: > 0 })
        {
            memoryColor = "#e34c26";
        }

        if (!current.DataSetColors.TryGetValue("Time", out var timeColor) ||
            timeColor is not { Length: > 0 })
        {
            timeColor = "#178600";
        }

        var options = System.Text.Json.JsonSerializer.Serialize(new
        {
            colors = new
            {
                memory = memoryColor,
                time = timeColor,
            },
            dataset = Items,
            errorBars = current.ErrorBars,
            name = Name,
            suiteName = Suite,
        });

        await JS.InvokeVoidAsync("renderChart", [ChartId, options]);

        if (firstRender)
        {
            await JS.InvokeVoidAsync("scrollToActiveChart", []);
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldRender() => true;
}
