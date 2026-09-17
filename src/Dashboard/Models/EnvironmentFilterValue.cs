// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a single selectable value for an <see cref="EnvironmentFilterOption"/>. This class cannot be inherited.
/// </summary>
/// <param name="RawValue">The raw JSON representation of the value, used to match against a benchmark run's metadata.</param>
/// <param name="DisplayValue">The human-readable representation of the value.</param>
public sealed record EnvironmentFilterValue(string RawValue, string DisplayValue);
