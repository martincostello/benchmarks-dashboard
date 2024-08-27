// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a Git user. This class cannot be inherited.
/// </summary>
public sealed class GitUser
{
    /// <summary>
    /// Gets or sets the user's name.
    /// </summary>
    [JsonPropertyName("username")]
    public string UserName { get; set; } = default!;
}
