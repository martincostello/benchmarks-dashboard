// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks.Layout;

public sealed partial class Navbar : IAsyncDisposable
{
    private string? _dataRepoUrl;

    /// <summary>
    /// Gets the <see cref="IJSRuntime"/> to use.
    /// </summary>
    [Inject]
    public required IJSRuntime JS { get; init; }

    /// <summary>
    /// Gets the dashboard options.
    /// </summary>
    private DashboardOptions Dashboard => Options.Value;

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GitHubService.OnUserChanged -= OnUserChanged;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var options = Options.Value;

        var uriBuilder = new UriBuilder(options.GitHubServerUrl)
        {
            Path = $"{options.RepositoryOwner}/{options.RepositoryName}",
        };

        _dataRepoUrl = uriBuilder.Uri.ToString();

        if (GitHubService.HasToken)
        {
            await GitHubService.VerifyTokenAsync();
        }

        GitHubService.OnUserChanged += OnUserChanged;
    }

    private void OnUserChanged(object? sender, GitHubUserChangedEventArgs args)
        => StateHasChanged();

    private async Task SignOutAsync()
    {
        await GitHubService.SignOutAsync();

        var uri = Options.Value.IsGitHubEnterprise ? Routes.Token : Routes.Home;
        Navigation.NavigateTo(uri);
    }

    private async Task ToggleThemeAsync()
        => await JS.InvokeVoidAsync("toggleTheme");
}
