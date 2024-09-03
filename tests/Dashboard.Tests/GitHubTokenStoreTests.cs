// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace MartinCostello.Benchmarks;

#pragma warning disable CA1849

public static class GitHubTokenStoreTests
{
    [Fact]
    public static async Task Can_Get_And_Set_Token()
    {
        // Arrange
        var storage = new LocalStorage();
        var options = Options.Create(new DashboardOptions() { GitHubServerUrl = new("https://github.local") });
        var target = new GitHubTokenStore(storage, storage, options);

        // Act
        var actual = target.GetToken();

        // Assert
        actual.ShouldBeNull();

        // Act
        actual = await target.GetTokenAsync();

        // Assert
        actual.ShouldBeNull();

        // Arrange
        await target.StoreTokenAsync("foo");

        // Assert
        actual = target.GetToken();
        actual.ShouldBe("foo");

        actual = await target.GetTokenAsync();
        actual.ShouldBe("foo");

        // Arrange
        await target.StoreTokenAsync(string.Empty);

        // Assert
        actual = target.GetToken();
        actual.ShouldBeEmpty();

        actual = await target.GetTokenAsync();
        actual.ShouldBeEmpty();
    }
}
