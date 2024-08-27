// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a single benchmark result. This class cannot be inherited.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>
    /// Gets or sets the name of the benchmark.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the value of the benchmark.
    /// </summary>
    [JsonPropertyName("value")]
    public double Value { get; set; } = default!;

    /// <summary>
    /// Gets or sets the value range of the benchmark.
    /// </summary>
    [JsonPropertyName("range")]
    public string? Range { get; set; }

    /// <summary>
    /// Gets or sets the unit of the benchmark.
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = default!;

    /// <summary>
    /// Gets or sets the number of bytes of memory allocated by the benchmark, if any.
    /// </summary>
    [JsonPropertyName("bytesAllocated")]
    public double? BytesAllocated { get; set; }

    /// <summary>
    /// Gets or sets the memory unit of the benchmark.
    /// </summary>
    [JsonPropertyName("memoryUnit")]
    public string? MemoryUnit { get; set; }
}
