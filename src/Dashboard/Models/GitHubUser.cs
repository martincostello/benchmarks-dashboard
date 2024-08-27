// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a GitHub user. This class cannot be inherited.
/// </summary>
public sealed class GitHubUser
{
    /// <summary>
    /// Gets or sets the user's GitHub avatar URL.
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user's GitHub login.
    /// </summary>
    [JsonPropertyName("login")]
    public string Login { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
}
