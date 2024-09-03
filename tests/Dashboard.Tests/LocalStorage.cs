// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Blazored.LocalStorage;

namespace MartinCostello.Benchmarks;

internal sealed class LocalStorage : ILocalStorageService, ISyncLocalStorageService
{
    private readonly Dictionary<string, string?> _storage = [];

#pragma warning disable CS0067
    public event EventHandler<ChangingEventArgs>? Changing;

    public event EventHandler<ChangedEventArgs>? Changed;
#pragma warning restore CS0067

    public void Clear() => _storage.Clear();

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        Clear();
        return ValueTask.CompletedTask;
    }

    public bool ContainKey(string key) => _storage.ContainsKey(key);

    public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = ContainKey(key);
        return ValueTask.FromResult(result);
    }

    public string? GetItemAsString(string key)
    {
        if (!_storage.TryGetValue(key, out var result))
        {
            result = null;
        }

        return result;
    }

    public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = GetItemAsString(key);
        return ValueTask.FromResult(result);
    }

    public void SetItemAsString(string key, string data)
        => _storage[key] = data;

    public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
    {
        SetItemAsString(key, data);
        return ValueTask.CompletedTask;
    }

    public T? GetItem<T>(string key)
    {
        throw new NotImplementedException();
    }

    public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public string? Key(int index)
        => throw new NotImplementedException();

    public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public IEnumerable<string> Keys()
        => throw new NotImplementedException();

    public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public int Length()
        => throw new NotImplementedException();

    public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public void RemoveItem(string key)
        => throw new NotImplementedException();

    public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public void RemoveItems(IEnumerable<string> keys)
        => throw new NotImplementedException();

    public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public void SetItem<T>(string key, T data)
        => throw new NotImplementedException();

    public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
