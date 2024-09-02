// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks.Layout;

public partial class MainLayout : LayoutComponentBase
{
    /// <summary>
    /// Gets the <see cref="IJSRuntime"/> to use.
    /// </summary>
    [Inject]
    public required IJSRuntime JS { get; init; }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await JS.InvokeVoidAsync("configureToolTips", []);
}
