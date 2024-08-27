// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a single benchmark run. This class cannot be inherited.
/// </summary>
public sealed class BenchmarkRun
{
    /// <summary>
    /// Gets or sets the commit associated with the benchmark run.
    /// </summary>
    [JsonPropertyName("commit")]
    public GitCommit Commit { get; set; } = default!;

    /// <summary>
    /// Gets or sets the date and time of the benchmark run.
    /// </summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(UnixEpochDateTimeOffsetConverter))]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets benchmark results for the run.
    /// </summary>
    [JsonPropertyName("benches")]
    public IList<BenchmarkResult> Benchmarks { get; set; } = [];
}
