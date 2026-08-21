// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing metadata associated with a benchmark run. This class cannot be inherited.
/// </summary>
public sealed class BenchmarkMetadata
{
    /// <summary>
    /// Gets or sets the metadata describing the environment the benchmark run was performed in, if any.
    /// </summary>
    [JsonPropertyName("environment")]
    public IDictionary<string, JsonElement>? Environment { get; set; }
}
