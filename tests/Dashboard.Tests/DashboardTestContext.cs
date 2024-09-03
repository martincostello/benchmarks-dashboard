// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Blazored.LocalStorage;
using Bunit;
using JustEat.HttpClientInterception;
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
            GitHubApiUrl = new("https://api.github.local"),
            GitHubApiVersion = "2022-11-28",
            GitHubClientId = "dkd73mfo9ASgjsfnhJD8",
            GitHubServerUrl = new("https://github.local"),
            Repositories = ["benchmarks-demo", "website"],
            RepositoryOwner = "martincostello",
            RepositoryName = "benchmarks",
            TokenScopes = ["public_repo"],
        };

        Services.AddSingleton((_) => Microsoft.Extensions.Options.Options.Create(Options));
        Services.AddSingleton((_) => Interceptor.CreateHttpClient());

        Services.AddSingleton<ILocalStorageService>(LocalStorage);
        Services.AddSingleton<ISyncLocalStorageService>(LocalStorage);

        Services.AddSingleton<GitHubClient>();
        Services.AddSingleton<GitHubService>();
        Services.AddSingleton<GitHubTokenStore>();
    }

    protected HttpClientInterceptorOptions Interceptor { get; }

    protected DashboardOptions Options { get; }

    private LocalStorage LocalStorage { get; }
}
