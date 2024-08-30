// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;

namespace MartinCostello.Benchmarks;

#pragma warning disable CA1848

/// <summary>
/// A class representing a service to acquire a GitHub device token. This class cannot be inherited.
/// </summary>
/// <param name="client">The GitHub client to use.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> to use.</param>
/// <param name="logger">The logger to use.</param>
public sealed class GitHubDeviceTokenService(
    GitHubClient client,
    TimeProvider timeProvider,
    ILogger<GitHubDeviceTokenService> logger)
{
    /// <summary>
    /// Gets a GitHub device code as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get a device code.
    /// </returns>
    public async Task<GitHubDeviceCode> GetDeviceCodeAsync(CancellationToken cancellationToken = default)
    {
        return await client.GetDeviceCodeAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for a GitHub access token as an asynchronous operation.
    /// </summary>
    /// <param name="deviceCode">The device code to use to acquire the access token.</param>
    /// <param name="cancellationToken">The optional <see cref="CancellationToken"/> to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to get an access token.
    /// </returns>
    public async Task<string?> WaitForAccessTokenAsync(GitHubDeviceCode deviceCode, CancellationToken cancellationToken = default)
    {
        var delay = TimeSpan.FromSeconds(deviceCode.RefreshIntervalInSeconds + 1);
        var expiry = timeProvider.GetUtcNow().AddSeconds(deviceCode.ExpiresInSeconds);

        while (timeProvider.GetUtcNow() < expiry)
        {
            var result = await client.GetAccessTokenAsync(deviceCode.DeviceCode, cancellationToken);

            if (result.AccessToken is { Length: > 0 } token)
            {
                return token;
            }

            switch (result.Error)
            {
                case "access_denied":
                    logger.LogInformation("The user cancelled the authorization process.");
                    return null;

                case "device_flow_disabled":
                    logger.LogWarning("The configured GitHub app has not enabled device flow.");
                    return null;

                case "expired_token":
                    logger.LogInformation("The device token has expired.");
                    return null;

                default:
                    break;
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Access token response: {Error}. Waiting {Interval} seconds.",
                    result.Error,
                    deviceCode.RefreshIntervalInSeconds);
            }

            await Task.Delay(delay, cancellationToken);
        }

        return null;
    }
}
