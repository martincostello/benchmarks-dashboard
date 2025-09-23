// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.Playwright;

namespace MartinCostello.Benchmarks.PageModels;

public abstract class AppPage(IPage page)
{
    protected IPage Page { get; } = page;

    public abstract Task WaitForContentAsync();

    public async Task<HomePage> HomeAsync()
    {
        await Page.ClickAsync(Selectors.HomeLink);
        return new(Page);
    }

    public async Task<TokenPage> SignInAsync()
    {
        await Page.ClickAsync(Selectors.SignIn);
        return new(Page);
    }

    public async Task SignOutAsync()
        => await Page.ClickAsync(Selectors.SignOut);

    public async Task ToggleThemeAsync()
        => await Page.ClickAsync(Selectors.ToggleTheme);

    public async Task<string> UserNameAsync()
        => (await Page.InnerTextAsync(Selectors.UserName)).Trim();

    public async Task WaitForSignedInAsync()
        => await Page.WaitForSelectorAsync(Selectors.UserName);

    public async Task WaitForSignedOutAsync()
        => await Assertions.Expect(Page.Locator(Selectors.SignIn))
                           .ToBeVisibleAsync();

    public abstract class Item(IElementHandle handle, IPage page)
    {
        protected IElementHandle Handle { get; } = handle;

        protected IPage Page { get; } = page;
    }

    private static class Selectors
    {
        internal const string HomeLink = "id=home-link";
        internal const string SignIn = "id=sign-in";
        internal const string SignOut = "id=sign-out";
        internal const string ToggleTheme = "id=toggle-theme";
        internal const string UserName = "id=user-name";
    }
}
