// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a single benchmark item. This class cannot be inherited.
/// </summary>
public sealed record BenchmarkItem(
    [property: JsonPropertyName("commit")] GitCommit Commit,
    [property: JsonPropertyName("result")] BenchmarkResult Result);
