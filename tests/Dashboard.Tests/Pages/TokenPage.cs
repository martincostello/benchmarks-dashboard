// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Playwright;

namespace MartinCostello.Benchmarks.Pages;

public sealed class TokenPage(IPage page) : AppPage(page)
{
    public override async Task WaitForContentAsync()
        => await Page.WaitForSelectorAsync(Selectors.UserCode);

    public async Task Authorize()
        => await Page.ClickAsync(Selectors.Authorize);

    public async Task RefreshUserCode()
        => await Page.ClickAsync(Selectors.RefreshCode);

    public async Task<bool> AuthorizationFailed()
    {
        await Page.WaitForSelectorAsync(Selectors.AuthorizationFailed);
        return await Page.IsVisibleAsync(Selectors.AuthorizationFailed);
    }

    public async Task<string> UserCode()
    {
        var element = await Page.WaitForSelectorAsync(Selectors.UserCode);
        element.ShouldNotBeNull();
        return await element.InputValueAsync();
    }

    private static class Selectors
    {
        internal const string Authorize = "id=authorize";
        internal const string AuthorizationFailed = "id=authorization-failed";
        internal const string RefreshCode = "id=refresh-code";
        internal const string UserCode = "id=user-code";
    }
}
