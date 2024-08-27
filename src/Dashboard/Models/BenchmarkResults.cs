// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing benchmark results for a GitHub repository branch.
/// </summary>
public sealed class BenchmarkResults
{
    /// <summary>
    /// Gets or sets the date and time the benchmark data was last updated.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    [JsonConverter(typeof(UnixEpochDateTimeOffsetConverter))]
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the URL of the repository the benchmark is associated with.
    /// </summary>
    [JsonPropertyName("repoUrl")]
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Gets or sets the benchmark suites.
    /// </summary>
    [JsonPropertyName("entries")]
    public IDictionary<string, IList<BenchmarkRun>> Suites { get; set; } = new Dictionary<string, IList<BenchmarkRun>>();
}
