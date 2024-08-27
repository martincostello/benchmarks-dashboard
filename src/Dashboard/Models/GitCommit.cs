// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a Git commit. This class cannot be inherited.
/// </summary>
public sealed class GitCommit
{
    /// <summary>
    /// Gets or sets the commit's author.
    /// </summary>
    [JsonPropertyName("author")]
    public GitUser Author { get; set; } = default!;

    /// <summary>
    /// Gets or sets the commit's committer.
    /// </summary>
    [JsonPropertyName("committer")]
    public GitUser Committer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the commit's SHA.
    /// </summary>
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = default!;

    /// <summary>
    /// Gets or sets the commit's message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;

    /// <summary>
    /// Gets or sets the date and time of the commit.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the commit's URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;
}
