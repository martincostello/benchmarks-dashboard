// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks;

/// <summary>
/// A fake <see cref="IJSRuntime"/> that emulates the browser's <c>localStorage</c>
/// API for use with <see cref="GitHubTokenStore"/> in tests. Any JS interop call
/// other than reading or writing local storage is delegated to the optionally
/// supplied fallback runtime, such as bUnit's own <see cref="IJSRuntime"/> fake.
/// </summary>
/// <param name="fallback">The optional fallback <see cref="IJSRuntime"/> to delegate other calls to.</param>
internal sealed class LocalStorage(IJSRuntime? fallback = null) : IJSInProcessRuntime
{
    private readonly Dictionary<string, string?> _storage = [];

    public TValue Invoke<TValue>(string identifier, params object?[]? args)
    {
        if (!TryGet(identifier, args, out TValue? result))
        {
            return ((IJSInProcessRuntime)Fallback()).Invoke<TValue>(identifier, args);
        }

        return result!;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (!TryGet(identifier, args, out TValue? result))
        {
            return Fallback().InvokeAsync<TValue>(identifier, args);
        }

        return ValueTask.FromResult(result!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (!TryGet(identifier, args, out TValue? result))
        {
            return Fallback().InvokeAsync<TValue>(identifier, cancellationToken, args);
        }

        return ValueTask.FromResult(result!);
    }

    private IJSRuntime Fallback()
        => fallback ?? throw new NotImplementedException($"No fallback {nameof(IJSRuntime)} was configured.");

    private bool TryGet<TValue>(string identifier, object?[]? args, out TValue? result)
    {
        switch (identifier)
        {
            case "localStorage.getItem":
                _storage.TryGetValue((string)args![0]!, out var value);
                result = (TValue?)(object?)value;
                return true;

            case "localStorage.setItem":
                _storage[(string)args![0]!] = (string)args[1]!;
                result = default;
                return true;

            default:
                result = default;
                return false;
        }
    }
}
