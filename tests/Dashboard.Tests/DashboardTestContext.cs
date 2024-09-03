// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using Blazored.LocalStorage;
using Bunit;
using JustEat.HttpClientInterception;
using MartinCostello.Benchmarks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MartinCostello.Benchmarks;

public abstract class DashboardTestContext : TestContext
{
    protected DashboardTestContext()
        : base()
    {
        Interceptor = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();
        LocalStorage = new();
        Options = new()
        {
            BenchmarkFileName = "data.json",
            BrandIcons = ["fa-solid fa-rocket"],
            GitHubApiUrl = new("https://api.github.local"),
            GitHubApiVersion = "2022-11-28",
            GitHubClientId = "dkd73mfo9ASgjsfnhJD8",
            GitHubTokenUrl = new("https://github.local"),
            GitHubServerUrl = new("https://github.local"),
            Repositories = ["benchmarks-demo", "website"],
            RepositoryOwner = "martincostello",
            RepositoryName = "benchmarks",
            TokenScopes = ["public_repo"],
        };

        Services.AddSingleton((_) => Microsoft.Extensions.Options.Options.Create(Options));
        Services.AddSingleton((_) => Interceptor.CreateHttpClient());

        Services.AddSingleton(TimeProvider.System);

        Services.AddSingleton<ILocalStorageService>(LocalStorage);
        Services.AddSingleton<ISyncLocalStorageService>(LocalStorage);

        Services.AddSingleton<GitHubDeviceTokenService>();
        Services.AddSingleton<GitHubClient>();
        Services.AddSingleton<GitHubService>();
        Services.AddSingleton<GitHubTokenStore>();
    }

    protected HttpClientInterceptorOptions Interceptor { get; }

    protected DashboardOptions Options { get; }

    private LocalStorage LocalStorage { get; }

    protected void UsingGitHubDotCom()
    {
        Options.GitHubApiUrl = new("https://api.github.com");
        Options.GitHubDataUrl = new("https://raw.githubusercontent.com");
        Options.GitHubServerUrl = new("https://github.com");
    }

    protected void WithBenchmarks(string repository, string branch)
    {
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{repository}", $"{repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{repository}/branches", $"{repository}-branches");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{repository}/data.json?ref={branch}", $"{repository}-{branch}");
    }

    protected void WithDeviceCode(GitHubDeviceCode deviceCode)
    {
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl($"https://github.local/login/device/code?client_id={Options.GitHubClientId}&scope=public_repo")
            .WithJsonContent(deviceCode);

        builder.RegisterWith(Interceptor);
    }

    protected async Task WithValidAccessToken(string accessToken = "VALID_GITHUB_ACCESS_TOKEN")
    {
        await Services.GetRequiredService<GitHubTokenStore>().StoreTokenAsync(accessToken);
        RegisterResponse("https://api.github.local/user", "user-valid-token");
    }

    protected void RegisterResponse(string url, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithStatus(statusCode)
            .WithContentStream(() => File.OpenRead(Path.Combine(".", "Responses", $"{name}.json")));

        builder.RegisterWith(Interceptor);
    }
}
