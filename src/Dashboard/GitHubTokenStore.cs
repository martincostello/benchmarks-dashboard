// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks;

/// <summary>
/// A class representing a store for GitHub authentication tokens. This class cannot be inherited.
/// </summary>
public sealed class GitHubTokenStore(
    IJSRuntime jsRuntime,
    IOptions<DashboardOptions> options)
{
    private string Key => $"github-token-{options.Value.GitHubServerUrl.Host}";

    /// <summary>
    /// Gets the GitHub token, if any, from local storage.
    /// </summary>
    /// <returns>
    /// The GitHub token in local storage, otherwise <see langword="null"/>.
    /// </returns>
    public string? GetToken()
        => ((IJSInProcessRuntime)jsRuntime).Invoke<string?>("localStorage.getItem", Key);

    /// <summary>
    /// Gets the GitHub token, if any, from local storage as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get the
    /// GitHub token in local storage, otherwise <see langword="null"/>.
    /// </returns>
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        => await jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, [Key]);

    /// <summary>
    /// Stores the specified GitHub token in local storage as an asynchronous operation.
    /// </summary>
    /// <param name="token">The GitHub token.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to store the token.
    /// </returns>
    public async Task StoreTokenAsync(string token, CancellationToken cancellationToken = default)
        => await jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, [Key, token]);
}
