// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

/// <summary>
/// A class representing a GitHub access token. This class cannot be inherited.
/// </summary>
public sealed class GitHubAccessToken
{
    /// <summary>
    /// Gets or sets the error code, if unsuccessful.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the type of the access token.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>
    /// Gets or sets the space-separated scopes(s) associated with the access token.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scopes { get; set; }
}
