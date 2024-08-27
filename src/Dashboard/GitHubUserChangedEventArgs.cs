// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;

namespace MartinCostello.Benchmarks;

/// <summary>
/// A class representing the event when the GitHub user changes.
/// </summary>
/// <param name="user">The GitHub user.</param>
public sealed class GitHubUserChangedEventArgs(GitHubUser? user) : EventArgs
{
    /// <summary>
    /// Gets the GitHub user, if any.
    /// </summary>
    public GitHubUser? User { get; } = user;
}
