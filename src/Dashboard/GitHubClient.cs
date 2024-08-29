﻿// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using MartinCostello.Benchmarks.Models;
using Microsoft.Extensions.Options;

namespace MartinCostello.Benchmarks;

/// <summary>
/// A class representing a GitHub client. This class cannot be inherited.
/// </summary>
public sealed class GitHubClient(
    HttpClient client,
    GitHubTokenStore tokenStore,
    IOptions<DashboardOptions> options)
{
    /// <summary>
    /// The GitHub device code grant type.
    /// </summary>
    /// <remarks>
    /// See https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#step-3-app-polls-github-to-check-if-the-user-authorized-the-device.
    /// </remarks>
    private const string GrantType = "urn:ietf:params:oauth:grant-type:device_code";

    /// <summary>
    /// Gets a GitHub device code as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get a device code.
    /// </returns>
    public async Task<GitHubDeviceCode> GetDeviceCodeAsync(CancellationToken cancellationToken = default)
    {
        var current = options.Value;

        var clientId = Uri.EscapeDataString(current.GitHubClientId);
        var scope = Uri.EscapeDataString(string.Join(' ', current.TokenScopes ?? ["public_repo"]));

        var relativeUri = $"login/device/code?client_id={clientId}&scope={scope}";
        var requestUri = new Uri(options.Value.GitHubTokenUrl, relativeUri);

        var response = await client.PostAsync(requestUri, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.GitHubDeviceCode, cancellationToken))!;
    }

    /// <summary>
    /// Gets a GitHub access token as an asynchronous operation.
    /// </summary>
    /// <param name="deviceCode">The device code to exchange an access code for.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get the access token.
    /// </returns>
    public async Task<GitHubAccessToken> GetAccessTokenAsync(
        string deviceCode,
        CancellationToken cancellationToken = default)
    {
        var current = options.Value;

        var clientId = Uri.EscapeDataString(current.GitHubClientId);
        var code = Uri.EscapeDataString(deviceCode);
        var grant = Uri.EscapeDataString(GrantType);

        var relativeUri = $"login/oauth/access_token?client_id={clientId}&device_code={code}&grant_type={grant}";
        var requestUri = new Uri(options.Value.GitHubTokenUrl, relativeUri);

        var response = await client.PostAsync(requestUri, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.GitHubAccessToken, cancellationToken))!;
    }

    /// <summary>
    /// Gets the benchmark data for the specified GitHub repository branch as an asynchronous operation.
    /// </summary>
    /// <param name="repository">The name of the repository.</param>
    /// <param name="branch">The name of the branch.</param>
    /// <param name="isPublic">Whether the visibility of the repository is public.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get the benchmark data.
    /// </returns>
    public async Task<BenchmarkResults?> GetBenchmarksAsync(
        string repository,
        string branch,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        var current = options.Value;
        var fileName = current.BenchmarkFileName;
        var useApi = current.IsGitHubEnterprise || !isPublic;
        var baseAddress = useApi ? current.GitHubApiUrl : current.GitHubDataUrl;

        var relativeUri =
            useApi ?
            $"repos/{current.RepositoryOwner}/{current.RepositoryName}/contents/{repository}/{fileName}?ref={branch}"
            : $"{current.RepositoryOwner}/{current.RepositoryName}/{branch}/{repository}/{fileName}";

        var requestUri = new Uri(baseAddress, relativeUri);

        using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);

        if (useApi)
        {
            await SetHeadersAsync(message.Headers, cancellationToken);
            message.Headers.Add("Accept", "application/vnd.github.v3.raw");
        }

        using var response = await client.SendAsync(message, cancellationToken);

        if (response.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.BenchmarkResults, cancellationToken))!;
    }

    /// <summary>
    /// Gets the specified GitHub repository as an asynchronous operation.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get the repository.
    /// </returns>
    public async Task<GitHubRepository> GetRepositoryAsync(string owner, string name, CancellationToken cancellationToken = default)
        => (await GetAsync($"repos/{owner}/{name}", AppJsonSerializerContext.Default.GitHubRepository, cancellationToken))!;

    /// <summary>
    /// Gets the specified GitHub repository's branches as an asynchronous operation.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="name">The name of the repository.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get the repository's branches.
    /// </returns>
    public async Task<IList<GitHubBranch>> GetRepositoryBranchesAsync(
        string owner,
        string name,
        CancellationToken cancellationToken = default)
        => await GetAsync($"repos/{owner}/{name}/branches", AppJsonSerializerContext.Default.IListGitHubBranch, cancellationToken) ?? [];

    /// <summary>
    /// Gets the current GitHub user as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get the login and name of the authenticated user.
    /// </returns>
    public async Task<GitHubUser> GetUserAsync(CancellationToken cancellationToken = default)
        => (await GetAsync("user", AppJsonSerializerContext.Default.GitHubUser, cancellationToken))!;

    private async Task<T?> GetAsync<T>(
        string url,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri(options.Value.GitHubApiUrl, url);

        using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
        await SetHeadersAsync(message.Headers, cancellationToken);

        using var response = await client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(
            jsonTypeInfo,
            cancellationToken);
    }

    private async Task SetHeadersAsync(HttpRequestHeaders headers, CancellationToken cancellationToken)
    {
        headers.Add("Accept", "application/vnd.github+json");
        headers.Add("X-GitHub-Api-Version", options.Value.GitHubApiVersion);

        var token = await tokenStore.GetTokenAsync(cancellationToken);

        if (!string.IsNullOrEmpty(token))
        {
            headers.Authorization = new("token", token);
        }
    }
}
