// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a GitHub repository branch. This class cannot be inherited.
/// </summary>
public sealed class GitHubBranch
{
    /// <summary>
    /// Gets or sets the name of the branch.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
}
