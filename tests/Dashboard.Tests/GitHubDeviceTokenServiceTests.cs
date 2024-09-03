// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;
using JustEat.HttpClientInterception;
using MartinCostello.Benchmarks.Models;
using Microsoft.Extensions.Time.Testing;

namespace MartinCostello.Benchmarks;

public class GitHubDeviceTokenServiceTests
{
    public GitHubDeviceTokenServiceTests(ITestOutputHelper outputHelper)
    {
        TimeProvider = new();
        Options = new()
        {
            GitHubClientId = "dkd73mfo9ASgjsfnhJD8",
            GitHubTokenUrl = new("https://github.local"),
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
        Target = new(client, TimeProvider, outputHelper.ToLogger<GitHubDeviceTokenService>());
    }

    private GitHubDeviceCode DeviceCode { get; } = new()
    {
        DeviceCode = "3584d83530557fdd1f46af8289938c8ef79f9dc5",
        ExpiresInSeconds = 900,
        RefreshIntervalInSeconds = 5,
        UserCode = "WDJB-MJHT",
        VerificationUrl = "https://github.local/login/device",
    };

    private HttpClientInterceptorOptions Interceptor { get; }

    private DashboardOptions Options { get; }

    private FakeTimeProvider TimeProvider { get; }

    private GitHubTokenStore TokenStore { get; }

    private GitHubDeviceTokenService Target { get; }

    [Fact]
    public async Task Can_Get_Device_Code()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl("https://github.local/login/device/code?client_id=dkd73mfo9ASgjsfnhJD8&scope=public_repo")
            .WithJsonContent(DeviceCode);

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
    public async Task Can_Get_Access_Token()
    {
        // Arrange
        var responses = new[]
        {
            Error("authorization_pending"),
            Error("authorization_pending"),
            Error("authorization_pending"),
            Error("authorization_pending"),
            AccessToken(),
        };

        int attempts = 0;

        RegisterAccessTokenResponse(() =>
        {
            var response = responses[attempts++];
            return JsonSerializer.SerializeToUtf8Bytes(response);
        });

        // Act
        var task = Target.WaitForAccessTokenAsync(DeviceCode);

        while (!task.IsCompleted)
        {
            TimeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        var actual = await task;

        // Assert
        actual.ShouldBe("not_a_real_token");
    }

    [Fact]
    public async Task Cannot_Get_Access_Token_When_Denied()
    {
        // Arrange
        var responses = new[]
        {
            Error("authorization_pending"),
            Error("authorization_pending"),
            Error("authorization_pending"),
            Error("access_denied"),
        };

        int attempts = 0;

        RegisterAccessTokenResponse(() =>
        {
            var response = responses[attempts++];
            return JsonSerializer.SerializeToUtf8Bytes(response);
        });

        // Act
        var task = Target.WaitForAccessTokenAsync(DeviceCode);

        while (!task.IsCompleted)
        {
            TimeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        var actual = await task;

        // Assert
        actual.ShouldBeNull();
    }

    [Fact]
    public async Task Cannot_Get_Access_Token_When_Expired()
    {
        // Arrange
        var responses = new[]
        {
            Error("authorization_pending"),
            Error("authorization_pending"),
            Error("authorization_pending"),
            Error("expired_token"),
        };

        int attempts = 0;

        RegisterAccessTokenResponse(() =>
        {
            var response = responses[attempts++];
            return JsonSerializer.SerializeToUtf8Bytes(response);
        });

        // Act
        var task = Target.WaitForAccessTokenAsync(DeviceCode);

        while (!task.IsCompleted)
        {
            TimeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        var actual = await task;

        // Assert
        actual.ShouldBeNull();
    }

    [Fact]
    public async Task Cannot_Get_Access_Token_When_Timed_Out()
    {
        // Arrange
        RegisterResponse(Error("authorization_pending"));

        // Act
        var task = Target.WaitForAccessTokenAsync(DeviceCode);

        while (!task.IsCompleted)
        {
            TimeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        var actual = await task;

        // Assert
        actual.ShouldBeNull();
    }

    [Fact]
    public async Task Cannot_Get_Access_Token_When_Device_Flow_Disabled()
    {
        // Arrange
        RegisterResponse(Error("device_flow_disabled"));

        // Act
        var actual = await Target.WaitForAccessTokenAsync(DeviceCode);

        // Assert
        actual.ShouldBeNull();
    }

    private static object AccessToken() =>
        new
        {
            access_token = "not_a_real_token",
            token_type = "bearer",
            scope = "public_repo",
        };

    private static object Error(string code) => new { error = code };

    private void RegisterResponse(object json)
        => RegisterAccessTokenResponse(() => JsonSerializer.SerializeToUtf8Bytes(json));

    private void RegisterAccessTokenResponse(Func<byte[]> contentFactory)
    {
        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl($"{Options.GitHubTokenUrl}login/oauth/access_token?client_id=dkd73mfo9ASgjsfnhJD8&device_code={DeviceCode.DeviceCode}&grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Adevice_code")
            .WithContent(contentFactory);

        builder.RegisterWith(Interceptor);
    }
}
