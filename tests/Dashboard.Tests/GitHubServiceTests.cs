// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using JustEat.HttpClientInterception;

namespace MartinCostello.Benchmarks;

public class GitHubServiceTests
{
    public GitHubServiceTests()
    {
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

        Interceptor = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();
        Interceptor.CreateHttpClient();

        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var storage = new LocalStorage();

        TokenStore = new GitHubTokenStore(
            storage,
            storage,
            options);

        var client = new GitHubClient(Interceptor.CreateHttpClient(), TokenStore, options);
        Target = new(client, TokenStore, options);
    }

    private HttpClientInterceptorOptions Interceptor { get; }

    private DashboardOptions Options { get; }

    private GitHubTokenStore TokenStore { get; }

    private GitHubService Target { get; }

    [Fact]
    public void HasToken_Is_Correct_No_Token()
    {
        // Act and Assert
        Target.HasToken.ShouldBeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("foo", true)]
    public async Task HasToken_Is_Correct_With_Token(string token, bool expected)
    {
        // Arrange
        await TokenStore.StoreTokenAsync(token, TestContext.Current.CancellationToken);

        // Act and Assert
        Target.HasToken.ShouldBe(expected);
    }

    [Fact]
    public void Repositories_Are_Correct()
    {
        // Act
        Target.Repositories.ShouldBe(["benchmarks-demo", "website"]);
    }

    [Fact]
    public async Task Can_Load_Benchmarks_Data()
    {
        // Assert
        Target.CurrentRepository.ShouldBeNull();

        Target.Branches.ShouldBe([]);
        Target.CurrentBranch.ShouldBeNull();
        Target.BranchUrl().ShouldBe("#");

        Target.CurrentCommit.ShouldBeNull();
        Target.CommitUrl().ShouldBe("#");

        Target.Benchmarks.ShouldBeNull();

        // Arrange
        string repository = "website";

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{repository}", $"{repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{repository}/branches", $"{repository}-branches");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{repository}/data.json?ref=main", $"{repository}-main");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{repository}/data.json?ref=dev", $"{repository}-dev");
        Interceptor.RegisterGet($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{repository}/data.json?ref=deleted", string.Empty, System.Net.HttpStatusCode.NotFound);

        // Act
        await Target.LoadRepositoryAsync(repository);

        // Assert
        Target.CurrentRepository.ShouldNotBeNull();
        Target.CurrentRepository.Name.ShouldBe(repository);

        Target.Branches.ShouldBe(["main", "dev"]);
        Target.CurrentBranch.ShouldBe("main");
        Target.BranchUrl().ShouldBe("https://github.local/martincostello/website/tree/main");

        Target.CurrentCommit.ShouldBe("349c16ff922077402de4777184582bb04d5d9e77");
        Target.CommitUrl().ShouldBe("https://github.local/martincostello/website/commits/349c16ff922077402de4777184582bb04d5d9e77");

        Target.Benchmarks.ShouldNotBeNull();
        Target.Benchmarks.Suites.ShouldContainKey("Website");
        Target.Benchmarks.Suites["Website"].Count.ShouldBe(24);

        // Arrange
        var previous = Target.Benchmarks;

        // Act
        await Target.LoadBenchmarksAsync("dev");

        // Assert
        Target.CurrentRepository.ShouldNotBeNull();
        Target.CurrentRepository.Name.ShouldBe(repository);

        Target.Branches.ShouldBe(["main", "dev"]);
        Target.CurrentBranch.ShouldBe("dev");
        Target.BranchUrl().ShouldBe("https://github.local/martincostello/website/tree/dev");

        Target.CurrentCommit.ShouldBe("323cbb3a8f1ee38ee935d876297f9e281e8b2c0f");
        Target.CommitUrl().ShouldBe("https://github.local/martincostello/website/commits/323cbb3a8f1ee38ee935d876297f9e281e8b2c0f");

        Target.Benchmarks.ShouldNotBeNull();
        Target.Benchmarks.ShouldNotBeSameAs(previous);
        Target.Benchmarks.Suites.ShouldContainKey("Website");
        Target.Benchmarks.Suites["Website"].Count.ShouldBe(25);

        // Act
        await Target.LoadBenchmarksAsync("deleted");

        // Assert
        Target.CurrentRepository.ShouldNotBeNull();
        Target.CurrentRepository.Name.ShouldBe(repository);

        Target.Branches.ShouldBe(["main", "dev"]);
        Target.CurrentBranch.ShouldBe("deleted");
        Target.BranchUrl().ShouldBe("https://github.local/martincostello/website/tree/deleted");

        Target.CurrentCommit.ShouldBeNull();
        Target.CommitUrl().ShouldBe("#");

        Target.Benchmarks.ShouldBeNull();

        // Arrange
        repository = "benchmarks-demo";

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{repository}", $"{repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{repository}/branches", $"{repository}-branches");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{repository}/data.json?ref=main", $"{repository}-main");

        // Act
        await Target.LoadRepositoryAsync(repository);

        // Assert
        Target.CurrentRepository.ShouldNotBeNull();
        Target.CurrentRepository.Name.ShouldBe(repository);

        Target.Branches.ShouldBe(["main", "dotnet-nightly", "dotnet-vnext"]);
        Target.CurrentBranch.ShouldBe("main");
        Target.BranchUrl().ShouldBe("https://github.local/martincostello/benchmarks-demo/tree/main");

        Target.CurrentCommit.ShouldBe("a4ba507a6e549244ec5142e47d65ac7f6abebc32");
        Target.CommitUrl().ShouldBe("https://github.local/martincostello/benchmarks-demo/commits/a4ba507a6e549244ec5142e47d65ac7f6abebc32");

        Target.Benchmarks.ShouldNotBeNull();
        Target.Benchmarks.Suites.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Can_Sign_In_And_Out()
    {
        // Act and Assert
        Target.CurrentUser.ShouldBeNull();
        Target.HasToken.ShouldBeFalse();
        Target.InvalidToken.ShouldBeFalse();
        Target.ConnectionUrl().ShouldBe("#");

        // Arrange
        RegisterResponse("https://api.github.local/user", "user-valid-token");

        bool signedIn = false;

        void AssertSignedIn(object? sender, GitHubUserChangedEventArgs args)
        {
            sender.ShouldBe(Target);
            args.ShouldNotBeNull();
            args.User.ShouldNotBeNull();
            args.User.Login.ShouldBe("speedy");
            signedIn = true;
        }

        Target.OnUserChanged += AssertSignedIn;

        // Act
        await Target.SignInAsync("foo", TestContext.Current.CancellationToken);

        // Assert
        signedIn.ShouldBeTrue();

        Target.CurrentUser.ShouldNotBeNull();
        Target.CurrentUser.Login.ShouldBe("speedy");

        Target.HasToken.ShouldBeTrue();
        Target.InvalidToken.ShouldBeFalse();
        Target.ConnectionUrl().ShouldBe("https://github.local/settings/connections/applications/dkd73mfo9ASgjsfnhJD8");

        (await TokenStore.GetTokenAsync(TestContext.Current.CancellationToken)).ShouldBe("foo");

        // Arrange
        bool signedOut = false;

        void AssertSignedOut(object? sender, GitHubUserChangedEventArgs args)
        {
            sender.ShouldBe(Target);
            args.ShouldNotBeNull();
            args.User.ShouldBeNull();
            signedOut = true;
        }

        Target.OnUserChanged -= AssertSignedIn;
        Target.OnUserChanged += AssertSignedOut;

        // Act
        await Target.SignOutAsync(TestContext.Current.CancellationToken);

        // Assert
        signedOut.ShouldBeTrue();

        Target.CurrentUser.ShouldBeNull();
        Target.HasToken.ShouldBeFalse();
        Target.InvalidToken.ShouldBeFalse();
        Target.ConnectionUrl().ShouldBe("#");
    }

    [Fact]
    public async Task Cannot_Sign_In_If_Token_Invalid()
    {
        // Act and Assert
        Target.CurrentUser.ShouldBeNull();
        Target.HasToken.ShouldBeFalse();
        Target.InvalidToken.ShouldBeFalse();
        Target.ConnectionUrl().ShouldBe("#");

        // Arrange
        RegisterResponse("https://api.github.local/user", "user-invalid-token", HttpStatusCode.NotFound);

        bool signedIn = false;

        void AssertSignedIn(object? sender, GitHubUserChangedEventArgs args)
            => signedIn = true;

        Target.OnUserChanged += AssertSignedIn;

        // Act
        await Target.SignInAsync("foo", TestContext.Current.CancellationToken);

        // Assert
        signedIn.ShouldBeFalse();

        Target.CurrentUser.ShouldBeNull();
        Target.HasToken.ShouldBeFalse();
        Target.InvalidToken.ShouldBeTrue();
        Target.ConnectionUrl().ShouldBe("#");
    }

    private void RegisterResponse(string url, string name, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithStatus(statusCode)
            .WithContentStream(() => File.OpenRead(Path.Combine(".", "Responses", $"{name}.json")));

        builder.RegisterWith(Interceptor);
    }
}
