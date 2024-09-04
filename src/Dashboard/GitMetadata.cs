// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace MartinCostello.Benchmarks;

/// <summary>
/// A class containing Git metadata for the application.
/// </summary>
public static class GitMetadata
{
    /// <summary>
    /// Gets the author of the application.
    /// </summary>
    public static string Author { get; } = GetMetadataValue("Author", "Martin Costello");

    /// <summary>
    /// Gets the ID of the build of the application.
    /// </summary>
    public static string BuildId { get; } = GetMetadataValue("BuildId", string.Empty);

    /// <summary>
    /// Gets the name of the branch of the application was built from.
    /// </summary>
    public static string Branch { get; } = GetMetadataValue("CommitBranch", "Unknown");

    /// <summary>
    /// Gets the Git SHA of the commit of the application was built from.
    /// </summary>
    public static string Commit { get; } = GetMetadataValue("CommitHash", "HEAD");

    /// <summary>
    /// Gets the URL of the GitHub repository.
    /// </summary>
    public static string RepositoryUrl { get; } = GetRepositoryUrl();

    /// <summary>
    /// Gets the date and time the application was built.
    /// </summary>
    public static DateTimeOffset Timestamp { get; } = DateTimeOffset.Parse(GetMetadataValue("BuildTimestamp", DateTimeOffset.UtcNow.ToString("u", CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the version number of the application.
    /// </summary>
    public static string Version { get; } = typeof(GitMetadata).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    private static string GetMetadataValue(string name, string defaultValue)
    {
        return typeof(GitMetadata).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where((p) => string.Equals(p.Key, name, StringComparison.Ordinal))
            .Select((p) => p.Value)
            .FirstOrDefault() ?? defaultValue;
    }

    private static string GetRepositoryUrl()
    {
        string repository = GetMetadataValue("RepositoryUrl", "https://github.com/martincostello/benchmarks-dashboard");

        const string Suffix = ".git";
        if (repository.EndsWith(Suffix, StringComparison.Ordinal))
        {
            repository = repository[..^Suffix.Length];
        }

        return repository;
    }
}
