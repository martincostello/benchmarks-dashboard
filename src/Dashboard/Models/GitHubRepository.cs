// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a GitHub repository. This class cannot be inherited.
/// </summary>
public sealed class GitHubRepository
{
    /// <summary>
    /// Gets or sets the name of the default branch of the repository.
    /// </summary>
    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = default!;

    /// <summary>
    /// Gets or sets the full name of the repository.
    /// </summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the HTML URL of the repository.
    /// </summary>
    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = default!;

    /// <summary>
    /// Gets or sets the name of the repository.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the owner of the repository.
    /// </summary>
    [JsonPropertyName("owner")]
    public GitHubUser Owner { get; set; } = default!;

    /// <summary>
    /// Gets or sets the visibility of the repository.
    /// </summary>
    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = default!;

    /// <summary>
    /// Gets a value indicating whether the repository is public.
    /// </summary>
    public bool IsPublic => Visibility == "public";
}
