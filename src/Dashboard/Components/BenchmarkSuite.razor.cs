// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;
using Microsoft.AspNetCore.Components;

namespace MartinCostello.Benchmarks.Components;

public partial class BenchmarkSuite
{
    /// <summary>
    /// Gets the benchmark suite name.
    /// </summary>
    [Parameter]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the benchmarks for the suite.
    /// </summary>
    [Parameter]
    public required Dictionary<string, IList<BenchmarkItem>> Benchmarks { get; init; }

    /// <inheritdoc/>
    protected override bool ShouldRender() => true;
}
