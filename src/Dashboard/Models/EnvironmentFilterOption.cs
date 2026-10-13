// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a filterable piece of environment metadata and its distinct values. This class cannot be inherited.
/// </summary>
/// <param name="Key">The name of the environment metadata.</param>
/// <param name="DisplayName">The human-readable name of the environment metadata.</param>
/// <param name="Values">The distinct values of the environment metadata that can be filtered on.</param>
public sealed record EnvironmentFilterOption(string Key, string DisplayName, IReadOnlyList<EnvironmentFilterValue> Values);
