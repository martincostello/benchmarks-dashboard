// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks;

public static class DashboardOptionsTests
{
    [Theory]
    [InlineData("https://api.github.com", false, "GitHub")]
    [InlineData("https://github.corp/api/v3", true, "GitHub Enterprise")]
    [InlineData("https://github.local/api/v3", true, "GitHub Enterprise")]
    public static void Determines_GitHub_Enterprise_Correctly(
        string githubApiUrl,
        bool expectedIsGitHubEnterprise,
        string expectedGitHubInstance)
    {
        // Arrange
        var target = new DashboardOptions()
        {
            GitHubApiUrl = new(githubApiUrl),
        };

        // Act and Assert
        target.IsGitHubEnterprise.ShouldBe(expectedIsGitHubEnterprise);
        target.GitHubInstance.ShouldBe(expectedGitHubInstance);
    }
}
