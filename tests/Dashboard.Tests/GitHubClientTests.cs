// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using JustEat.HttpClientInterception;

namespace MartinCostello.Benchmarks;

public class GitHubClientTests
{
    public GitHubClientTests()
    {
        Options = new()
        {
            BenchmarkFileName = "data.json",
            GitHubApiUrl = new("https://api.github.local"),
            GitHubApiVersion = "2022-11-28",
            GitHubClientId = "dkd73mfo9ASgjsfnhJD8",
            GitHubDataUrl = new("https://raw.githubusercontent.local"),
            GitHubTokenUrl = new("https://github.local"),
            RepositoryOwner = "octocat",
            RepositoryName = "benchmarks",
            TokenScopes = ["public_repo"],
        };

        Interceptor = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();
        Interceptor.CreateHttpClient();

        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var storage = new LocalStorage();

        TokenStore = new GitHubTokenStore(
            storage,
            storage,
            options);

        Target = new(Interceptor.CreateHttpClient(), TokenStore, options);
    }

    private HttpClientInterceptorOptions Interceptor { get; }

    private DashboardOptions Options { get; }

    private GitHubTokenStore TokenStore { get; }

    private GitHubClient Target { get; }

    [Fact]
    public async Task Can_Get_Device_Code()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl("https://github.local/login/device/code?client_id=dkd73mfo9ASgjsfnhJD8&scope=public_repo")
            .WithJsonContent(
                new
                {
                    device_code = "3584d83530557fdd1f46af8289938c8ef79f9dc5",
                    expires_in = 900,
                    interval = 5,
                    user_code = "WDJB-MJHT",
                    verification_uri = "https://github.local/login/device",
                });

        builder.RegisterWith(Interceptor);

        // Act
        var actual = await Target.GetDeviceCodeAsync();

        // Assert
        actual.ShouldNotBeNull();
        actual.DeviceCode.ShouldBe("3584d83530557fdd1f46af8289938c8ef79f9dc5");
        actual.ExpiresInSeconds.ShouldBe(900);
        actual.RefreshIntervalInSeconds.ShouldBe(5);
        actual.UserCode.ShouldBe("WDJB-MJHT");
        actual.VerificationUrl.ShouldBe("https://github.local/login/device");
    }

    [Fact]
    public async Task Can_Get_Access_Code()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl("https://github.local/login/oauth/access_token?client_id=dkd73mfo9ASgjsfnhJD8&device_code=3584d83530557fdd1f46af8289938c8ef79f9dc5&grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Adevice_code")
            .WithJsonContent(
                new
                {
                    access_token = "not_a_real_token",
                    token_type = "bearer",
                    scope = "public_repo",
                });

        builder.RegisterWith(Interceptor);

        // Act
        var actual = await Target.GetAccessTokenAsync("3584d83530557fdd1f46af8289938c8ef79f9dc5");

        // Assert
        actual.ShouldNotBeNull();
        actual.AccessToken.ShouldBe("not_a_real_token");
        actual.Error.ShouldBeNull();
        actual.TokenType.ShouldBe("bearer");
        actual.Scopes.ShouldBe("public_repo");
    }

    [Fact]
    public async Task Can_Get_Benchmarks_Public_GitHub_Repository()
    {
        // Arrange
        Options.GitHubApiUrl = new("https://api.github.com");

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://raw.githubusercontent.local/octocat/benchmarks/my-branch/my-repository/data.json")
            .WithJsonContent(
                new
                {
                    lastUpdated = 1725355699000,
                    repoUrl = "https://github.local/octocat/my-repository",
                });

        builder.RegisterWith(Interceptor);

        string repository = "my-repository";
        string branch = "my-branch";
        bool isPublic = true;

        // Act
        var actual = await Target.GetBenchmarksAsync(repository, branch, isPublic);

        // Assert
        actual.ShouldNotBeNull();
        actual.LastUpdated.ShouldBe(new(2024, 09, 03, 09, 28, 19, TimeSpan.Zero));
        actual.RepositoryUrl.ShouldBe("https://github.local/octocat/my-repository");
    }

    [Fact]
    public async Task Can_Get_Benchmarks_Private_GitHub_Repository()
    {
        // Arrange
        await TokenStore.StoreTokenAsync("foo");

        Options.GitHubApiUrl = new("https://api.github.com");

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://api.github.com/repos/octocat/benchmarks/contents/my-repository/data.json?ref=my-branch")
            .ForRequestHeader("Accept", "application/vnd.github.v3.raw")
            .ForRequestHeader("Authorization", "token foo")
            .ForRequestHeader("X-GitHub-Api-Version", "2022-11-28")
            .WithJsonContent(
                new
                {
                    lastUpdated = 1725355699000,
                    repoUrl = "https://github.local/octocat/my-repository",
                });

        builder.RegisterWith(Interceptor);

        string repository = "my-repository";
        string branch = "my-branch";
        bool isPublic = false;

        // Act
        var actual = await Target.GetBenchmarksAsync(repository, branch, isPublic);

        // Assert
        actual.ShouldNotBeNull();
        actual.LastUpdated.ShouldBe(new(2024, 09, 03, 09, 28, 19, TimeSpan.Zero));
        actual.RepositoryUrl.ShouldBe("https://github.local/octocat/my-repository");
    }

    [Theory]
    [CombinatorialData]
    public async Task Can_Get_Benchmarks_GitHub_Enterprise_Repository(bool isPublic)
    {
        // Arrange
        await TokenStore.StoreTokenAsync("foo");

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://api.github.local/repos/octocat/benchmarks/contents/my-repository/data.json?ref=my-branch")
            .ForRequestHeader("Accept", "application/vnd.github.v3.raw")
            .ForRequestHeader("Authorization", "token foo")
            .ForRequestHeader("X-GitHub-Api-Version", "2022-11-28")
            .WithJsonContent(
                new
                {
                    lastUpdated = 1725355699000,
                    repoUrl = "https://github.local/octocat/my-repository",
                });

        builder.RegisterWith(Interceptor);

        string repository = "my-repository";
        string branch = "my-branch";

        // Act
        var actual = await Target.GetBenchmarksAsync(repository, branch, isPublic);

        // Assert
        actual.ShouldNotBeNull();
        actual.LastUpdated.ShouldBe(new(2024, 09, 03, 09, 28, 19, TimeSpan.Zero));
        actual.RepositoryUrl.ShouldBe("https://github.local/octocat/my-repository");
    }

    [Fact]
    public async Task Can_Get_Benchmarks_Returns_Null_If_Not_Found()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://api.github.local/repos/octocat/benchmarks/contents/my-repository/data.json?ref=my-branch")
            .WithStatus(404);

        builder.RegisterWith(Interceptor);

        string repository = "my-repository";
        string branch = "my-branch";
        bool isPublic = true;

        // Act
        var actual = await Target.GetBenchmarksAsync(repository, branch, isPublic);

        // Assert
        actual.ShouldBeNull();
    }

    [Theory]
    [InlineData("public", true)]
    [InlineData("internal", false)]
    [InlineData("private", false)]
    public async Task Can_Get_Repository(string visibility, bool expectedIsPublic)
    {
        // Arrange
        await TokenStore.StoreTokenAsync("foo");

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://api.github.local/repos/octocat/my-repository")
            .ForRequestHeader("Accept", "application/vnd.github+json")
            .ForRequestHeader("X-GitHub-Api-Version", "2022-11-28")
            .WithJsonContent(
                new
                {
                    default_branch = "main",
                    full_name = "octocat/my-repository",
                    html_url = "https://github.local/octocat/my-repository",
                    name = "my-repository",
                    owner = new
                    {
                        login = "octocat",
                    },
                    visibility,
                });

        builder.RegisterWith(Interceptor);

        string owner = "octocat";
        string repository = "my-repository";

        // Act
        var actual = await Target.GetRepositoryAsync(owner, repository);

        // Assert
        actual.ShouldNotBeNull();
        actual.DefaultBranch.ShouldBe("main");
        actual.FullName.ShouldBe("octocat/my-repository");
        actual.HtmlUrl.ShouldBe("https://github.local/octocat/my-repository");
        actual.Name.ShouldBe("my-repository");
        actual.Owner.ShouldNotBeNull();
        actual.Owner.Login.ShouldBe("octocat");
        actual.Visibility.ShouldBe(visibility);
        actual.IsPublic.ShouldBe(expectedIsPublic);
    }

    [Fact]
    public async Task Can_Get_Branches()
    {
        // Arrange
        await TokenStore.StoreTokenAsync("foo");

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://api.github.local/repos/octocat/my-repository/branches")
            .ForRequestHeader("Accept", "application/vnd.github+json")
            .ForRequestHeader("X-GitHub-Api-Version", "2022-11-28")
            .WithJsonContent(
                new[]
                {
                    new { name = "main" },
                    new { name = "my-new-feature" },
                });

        builder.RegisterWith(Interceptor);

        string owner = "octocat";
        string repository = "my-repository";

        // Act
        var actual = await Target.GetRepositoryBranchesAsync(owner, repository);

        // Assert
        actual.ShouldNotBeNull();
        actual.Count.ShouldBe(2);
        actual.ShouldAllBe((p) => p != null);
        actual.Select((p) => p.Name).ShouldBe(["main", "my-new-feature"]);
    }

    [Fact]
    public async Task Can_Get_User()
    {
        // Arrange
        Options.GitHubApiUrl = new("https://github.local/api/v3/");

        await TokenStore.StoreTokenAsync("foo");

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl("https://github.local/api/v3/user")
            .ForRequestHeader("Accept", "application/vnd.github+json")
            .ForRequestHeader("X-GitHub-Api-Version", "2022-11-28")
            .WithJsonContent(
                new
                {
                    avatar_url = "https://avatars.githubusercontent.com/u/583231?v=4",
                    login = "octocat",
                    name = "The Octocat",
                });

        builder.RegisterWith(Interceptor);

        // Act
        var actual = await Target.GetUserAsync();

        // Assert
        actual.ShouldNotBeNull();
        actual.AvatarUrl.ShouldBe("https://avatars.githubusercontent.com/u/583231?v=4");
        actual.Login.ShouldBe("octocat");
        actual.Name.ShouldBe("The Octocat");
    }
}
