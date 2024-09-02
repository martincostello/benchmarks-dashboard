// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks.Pages;

public partial class Token
{
    private bool _authorizing;
    private bool _authorizationFailed;
    private bool _tokenGenerationFailed;
    private GitHubDeviceCode? _deviceCode;

    /// <summary>
    /// Gets the <see cref="IJSRuntime"/> to use.
    /// </summary>
    [Inject]
    public required IJSRuntime JS { get; init; }

    /// <summary>
    /// Gets the <see cref="GitHubDeviceTokenService"/> to use.
    /// </summary>
    [Inject]
    public required GitHubDeviceTokenService TokenService { get; init; }

    private DashboardOptions Dashboard => Options.Value;

    private bool TokenRequired => Dashboard.IsGitHubEnterprise;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
        => await GenerateCodeAsync();

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_deviceCode is not null)
        {
            await JS.InvokeVoidAsync("configureClipboard");
        }
    }

    private async Task GenerateCodeAsync()
    {
        try
        {
            _deviceCode = await TokenService.GetDeviceCodeAsync();
            _tokenGenerationFailed = false;
        }
        catch (HttpRequestException)
        {
            _tokenGenerationFailed = true;
        }
    }

    private async Task RefreshCodeAsync()
    {
        _deviceCode = null;

        await GenerateCodeAsync();

        _authorizing = false;
        _authorizationFailed = false;

        StateHasChanged();
    }

    private async Task AuthorizeAsync()
    {
        if (_deviceCode is null)
        {
            return;
        }

        _authorizing = true;

        StateHasChanged();

        if (await TokenService.WaitForAccessTokenAsync(_deviceCode) is { Length: > 0 } token &&
            await GitHubService.SignInAsync(token))
        {
            Navigation.NavigateTo(Routes.Home);
        }

        StateHasChanged();

        _authorizing = false;
        _authorizationFailed = true;
    }
}
