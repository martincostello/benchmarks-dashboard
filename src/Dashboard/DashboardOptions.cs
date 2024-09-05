// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks;

/// <summary>
/// A class representing the configuration options for the dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Gets or sets the name of the benchmark data files.
    /// </summary>
    public string BenchmarkFileName { get; set; } = "data.json";

    /// <summary>
    /// Gets or sets the brand name for the dashboard.
    /// </summary>
    public string BrandName { get; set; } = "Benchmarks";

    /// <summary>
    /// Gets or sets the Font Awesome icons classes to use for the dashboard brand.
    /// </summary>
    public IList<string> BrandIcons { get; set; } = [];

    /// <summary>
    /// Gets or sets the colors to use for the chart data sets.
    /// </summary>
    public IDictionary<string, string> DataSetColors { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a value indicating whether to show error bars.
    /// </summary>
    public bool ErrorBars { get; set; } = true;

    /// <summary>
    /// Gets or sets the URI to use for the GitHub API.
    /// </summary>
    public Uri GitHubApiUrl { get; set; } = new("https://api.github.com");

    /// <summary>
    /// Gets or sets the GitHub API version to use for requests.
    /// </summary>
    public string GitHubApiVersion { get; set; } = "2022-11-28";

    /// <summary>
    /// Gets or sets the GitHub client ID to use to authenticate using the device flow.
    /// </summary>
    public string GitHubClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URI to use for the GitHub data.
    /// </summary>
    public Uri GitHubDataUrl { get; set; } = new("https://raw.githubusercontent.com");

    /// <summary>
    /// Gets or sets the URI to use for the GitHub server.
    /// </summary>
    public Uri GitHubServerUrl { get; set; } = new("https://github.com");

    /// <summary>
    /// Gets or sets the URI to use for acquiring GitHub tokens.
    /// </summary>
    public Uri GitHubTokenUrl { get; set; } = new("https://api.martincostello.com/github/");

    /// <summary>
    /// Gets or sets the format to save chart images as.
    /// </summary>
    public string ImageFormat { get; set; } = "png";

    /// <summary>
    /// Gets or sets the owner of the dashboard repository.
    /// </summary>
    public string RepositoryOwner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the dashboard repository.
    /// </summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the names of the repositories to show charts for.
    /// </summary>
    public IList<string> Repositories { get; set; } = [];

    /// <summary>
    /// Gets or sets the GitHub token scope(s) to request.
    /// </summary>
    public IList<string> TokenScopes { get; set; } = ["public_repo"];

    /// <summary>
    /// Gets a value indicating whether GitHub Enterprise is being used.
    /// </summary>
    public bool IsGitHubEnterprise => GitHubApiUrl.Host != "api.github.com";

    /// <summary>
    /// Gets the name to use to refer to the GitHub instance.
    /// </summary>
    public string GitHubInstance => IsGitHubEnterprise ? "GitHub Enterprise" : "GitHub";
}
