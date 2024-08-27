// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Blazored.LocalStorage;
using MartinCostello.Benchmarks.Models;
using Microsoft.Extensions.Options;

namespace MartinCostello.Benchmarks;

/// <summary>
/// A class representing a service for interacting with GitHub.
/// </summary>
public sealed class GitHubService(
    GitHubClient client,
    ISyncLocalStorageService localStorage,
    IOptions<DashboardOptions> options)
{
    private const string TokenKey = "github-token";

    private readonly List<string> _branches = [];
    private readonly string[] _repositories = [.. options.Value.Repositories];
    private bool _invalidToken;

    /// <summary>
    /// Raised when the GitHub user changes.
    /// </summary>
    public event EventHandler<GitHubUserChangedEventArgs>? OnUserChanged;

    /// <summary>
    /// Gets the benchmark results for the current repository and branch.
    /// </summary>
    public BenchmarkResults? Benchmarks { get; private set; }

    /// <summary>
    /// Gets the name of the current branch.
    /// </summary>
    public string? CurrentBranch { get; private set; }

    /// <summary>
    /// Gets the SHA of the current Git commit.
    /// </summary>
    public string? CurrentCommit { get; private set; }

    /// <summary>
    /// Gets the current GitHub repository, if any.
    /// </summary>
    public GitHubRepository? CurrentRepository { get; private set; }

    /// <summary>
    /// Gets the current GitHub user, if any.
    /// </summary>
    public GitHubUser? CurrentUser { get; private set; }

    /// <summary>
    /// Gets a value indicating whether has a GitHub token configured.
    /// </summary>
    public bool HasToken => !string.IsNullOrEmpty(localStorage.GetItemAsString(TokenKey));

    /// <summary>
    /// Gets a value indicating whether the token is invalid.
    /// </summary>
    public bool InvalidToken => _invalidToken;

    /// <summary>
    /// Gets the GitHub branches associated with <see cref="CurrentRepository"/>.
    /// </summary>
    public IReadOnlyList<string> Branches => [.. _branches];

    /// <summary>
    /// Gets the configured GitHub repositories.
    /// </summary>
    public IReadOnlyList<string> Repositories => _repositories;

    /// <summary>
    /// Gets the URL for the current branch.
    /// </summary>
    /// <returns>
    /// The URL for the current branch.
    /// </returns>
    public string BranchUrl()
    {
        if (CurrentRepository is not { } repo || CurrentBranch is not { } branch)
        {
            return "#";
        }

        var current = options.Value;

        var builder = new UriBuilder(current.GitHubServerUrl)
        {
            Path = $"{current.RepositoryOwner}/{repo.Name}/tree/{branch}",
        };

        return builder.Uri.ToString();
    }

    /// <summary>
    /// Gets the URL for the current commit.
    /// </summary>
    /// <returns>
    /// The URL for the current commit.
    /// </returns>
    public string CommitUrl()
    {
        if (CurrentRepository is not { } repo || CurrentCommit is not { } sha)
        {
            return "#";
        }

        var current = options.Value;

        var builder = new UriBuilder(current.GitHubServerUrl)
        {
            Path = $"{current.RepositoryOwner}/{repo.Name}/commits/{sha}",
        };

        return builder.Uri.ToString();
    }

    /// <summary>
    /// Loads the benchmarks for the specified branch as an asynchronous operation.
    /// </summary>
    /// <param name="branch">The branch to load the benchmarks for.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to load the benchmarks.
    /// </returns>
    public async Task<bool> LoadBenchmarksAsync(string branch)
    {
        if (CurrentRepository is null)
        {
            throw new InvalidOperationException("No current repository.");
        }

        CurrentBranch = branch;
        Benchmarks = await client.GetBenchmarksAsync(CurrentRepository.Name, CurrentBranch, CurrentRepository.IsPublic);

        if (Benchmarks is null)
        {
            return false;
        }

        if (Benchmarks.Suites.Count > 0)
        {
            var commits = new HashSet<string>();

            foreach (var benchmarks in Benchmarks.Suites.Values.Where((p) => p.Count > 0))
            {
                commits.Add(benchmarks[benchmarks.Count - 1].Commit.Sha);
            }

            if (commits.Count == 1)
            {
                CurrentCommit = commits.FirstOrDefault();
            }
        }

        return true;
    }

    /// <summary>
    /// Loads the data for the specified repository as an asynchronous operation.
    /// </summary>
    /// <param name="repository">The repository to load the data for.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to load the repository.
    /// </returns>
    public async Task LoadRepositoryAsync(string repository)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);

        CurrentRepository = await client.GetRepositoryAsync(options.Value.RepositoryOwner, repository);

        await LoadBranchesAsync(CurrentRepository);

        if (_branches?.Count > 0)
        {
            CurrentBranch = _branches[0];
            await LoadBenchmarksAsync(CurrentBranch);
        }
    }

    /// <summary>
    /// Signs in the user with the specified GitHub token as an asynchronous operation.
    /// </summary>
    /// <param name="token">The GitHub token to sign in with.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to sign in that returns
    /// <see langword="true"/> if the specified token is valid; otherwise <see langword="false"/>.
    /// </returns>
    public async Task<bool> SignInAsync(string token, CancellationToken cancellationToken = default)
    {
        localStorage.SetItemAsString(TokenKey, token);

        if (await VerifyTokenAsync(cancellationToken))
        {
            OnUserChanged?.Invoke(this, new(CurrentUser));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Signs out the user.
    /// </summary>"Task"/> representing the asynchronous operation to sign out.
    /// </returns>
    public void SignOut()
    {
        localStorage.SetItemAsString(TokenKey, string.Empty);

        CurrentUser = null;
        OnUserChanged?.Invoke(this, new(null));
    }

    /// <summary>
    /// Verifies the current user's access token as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to verify the token
    /// that returns <see langword="true"/> if the token is valid; otherwise <see langword="false"/>.
    /// </returns>
    public async Task<bool> VerifyTokenAsync(CancellationToken cancellationToken = default)
    {
        bool result;

        try
        {
            CurrentUser = await client.GetUserAsync(cancellationToken);
            result = true;
        }
        catch (HttpRequestException)
        {
            SignOut();
            result = false;
        }

        _invalidToken = !result;
        return result;
    }

    private async Task LoadBranchesAsync(GitHubRepository repository)
    {
        var branches = await client.GetRepositoryBranchesAsync(options.Value.RepositoryOwner, repository.Name);

        _branches.Clear();
        foreach (var branch in branches)
        {
            _branches.Add(branch.Name);
        }

        _branches.Sort((x, y) =>
        {
            if (x == repository.DefaultBranch)
            {
                return -1;
            }
            else if (y == repository.DefaultBranch)
            {
                return 1;
            }

            return string.Compare(x, y, StringComparison.Ordinal);
        });
    }
}
