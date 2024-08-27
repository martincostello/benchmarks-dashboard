// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Playwright;

namespace MartinCostello.Benchmarks.Pages;

public sealed class TokenPage(IPage page) : AppPage(page)
{
    public override async Task WaitForContentAsync()
        => await Page.WaitForSelectorAsync(Selectors.TokenInput);

    public async Task<bool> TokenIsInvalid()
    {
        await Page.WaitForSelectorAsync(Selectors.InvalidToken);
        return await Page.IsVisibleAsync(Selectors.InvalidToken);
    }

    public async Task<TokenPage> WithToken(string token)
    {
        await Page.FillAsync(Selectors.TokenInput, token);
        return this;
    }

    public async Task SaveToken()
        => await Page.ClickAsync(Selectors.SaveButton);

    private static class Selectors
    {
        internal const string InvalidToken = "id=invalid-token";
        internal const string TokenInput = "id=token-input";
        internal const string SaveButton = "id=save-token";
    }
}
