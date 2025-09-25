// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;
using MartinCostello.Benchmarks.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks.Pages;

public partial class Home
{
    private bool _loading = true;
    private bool _notFound;

    /// <summary>
    /// Gets a value indicating whether to disable the repositories input.
    /// </summary>
    public bool DisableRepositories => GitHubService.Repositories.Count < 1 || _loading;

    /// <summary>
    /// Gets a value indicating whether to disable the branches input.
    /// </summary>
    public bool DisableBranches => GitHubService.Branches.Count < 1 || _loading;

    /// <summary>
    /// Gets the currently selected branch, if any.
    /// </summary>
    public string? SelectedBranch =>
        Branch ??
        GitHubService.CurrentBranch ??
        GitHubService.CurrentRepository?.DefaultBranch;

    /// <summary>
    /// Gets a value indicating whether to show the loading indicators.
    /// </summary>
    public bool ShowLoaders => _loading;

    /// <summary>
    /// Gets or sets the branch specified by the query string, if any.
    /// </summary>
    [SupplyParameterFromQuery(Name = "branch")]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the repository specified by the query string, if any.
    /// </summary>
    [SupplyParameterFromQuery(Name = "repo")]
    public string? Repository { get; set; }

    /// <summary>
    /// Gets the <see cref="IJSRuntime"/> to use.
    /// </summary>
    [Inject]
    public required IJSRuntime JS { get; init; }

    /// <summary>
    /// Groups the specified benchmark runs into their respective benchmark methods.
    /// </summary>
    /// <param name="runs">The benchmark runs to summarize.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing the benchmark items for
    /// each benchmark in the specified benchmark runs.
    /// </returns>
    public static Dictionary<string, IList<BenchmarkItem>> GroupBenchmarks(IList<BenchmarkRun> runs)
    {
        Dictionary<string, SortedList<DateTimeOffset, BenchmarkItem>> sortedGroups = [];

        foreach (var run in runs.DistinctBy((p) => p.Commit.Sha))
        {
            foreach (var benchmark in run.Benchmarks)
            {
                TryAdd(benchmark.Name);
                continue;

                void TryAdd(string key)
                {
                    if (!sortedGroups.TryGetValue(key, out var results))
                    {
                        sortedGroups[key] = results = [];
                    }

                    // We have multiple runs for the same commit, they may be different jobs.
                    // By default the names are not unique, so lets append a suffix to make them so.
                    // This assumes that they will be in the same order each time.
                    if (results.ContainsKey(run.Timestamp))
                    {
                        // check if the key already ends with '[0-9]'
                        var match = SuffixRegex().Match(key);
                        if (match.Success)
                        {
                            // if it does, increment the number
                            var baseKey = match.Groups[1].Value;
                            var number = match.Groups[3].Success switch
                            {
                                true => int.Parse(match.Groups[3].Value, NumberFormatInfo.InvariantInfo) + 1,
                                false => 1,
                            };
                            key = $"{baseKey}[{number}]";
                        }
                        else
                        {
                            key = $"{key}[1]";
                        }

                        TryAdd(key);
                    }
                    else
                    {
                        results.Add(run.Timestamp, new(run.Commit, benchmark));
                    }
                }
            }
        }

        Dictionary<string, IList<BenchmarkItem>> grouped = [];

        foreach ((var suite, var results) in sortedGroups)
        {
            var items = results.Select((p) => p.Value).ToList();

            NormalizeUnits(items);

            grouped[suite] = items;
        }

        return grouped;
    }

    /// <summary>
    /// Normalizes the units of the specified benchmarks to use the same unit.
    /// </summary>
    /// <param name="items">The items to normalize.</param>
    public static void NormalizeUnits(IList<BenchmarkItem> items)
    {
        if (items.Count < 1)
        {
            return;
        }

        const double Factor = 1e-3;

        // This preserves values < 700 in the current unit.
        // Allows 0.7+ values to scale up to the next unit.
        const double Limit = 700;

        var hasAllocations = items.Any((p) => p.Result.BytesAllocated is not null);

        // Normalize input allocations
        foreach (var item in items)
        {
            // normalize to nanoseconds
            item.Result.Value = item.Result.Unit switch
            {
                null or "ns" => item.Result.Value,
                "µs" => item.Result.Value * 1e3,
                "ms" => item.Result.Value * 1e6,
                "s" => item.Result.Value * 1e9,
                _ => throw new ArgumentOutOfRangeException(nameof(items), item.Result.Unit, "Unknown time unit."),
            };

            if (double.TryParse(item.Result.Range?[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var rangeDouble))
            {
                var prefix = item.Result.Range[0..2];
                var updated = item.Result.Unit switch
                {
                    null or "ns" => rangeDouble,
                    "µs" => rangeDouble * 1e3,
                    "ms" => rangeDouble * 1e6,
                    "s" => rangeDouble * 1e9,
                    _ => throw new ArgumentOutOfRangeException(nameof(items), item.Result.Unit, "Unknown time unit."),
                };

                item.Result.Range = prefix + updated;
            }

            item.Result.Unit = "ns";

            if (item.Result.BytesAllocated is not null)
            {
                // normalize to bytes
                item.Result.BytesAllocated = item.Result.MemoryUnit switch
                {
                    // When it's bytes it's sometimes not provided.
                    null or "B" => item.Result.BytesAllocated,
                    "KB" => item.Result.BytesAllocated * 1e3,
                    "MB" => item.Result.BytesAllocated * 1e6,
                    "GB" => item.Result.BytesAllocated * 1e9,
                    "TB" => item.Result.BytesAllocated * 1e12,
                    _ => throw new ArgumentOutOfRangeException(nameof(items), item.Result.MemoryUnit, "Unknown memory unit."),
                };
            }

            // I'm not sure how real this will be, but there was an edge case
            // in the tests where 1 entry was missing allocations.
            if (hasAllocations)
            {
                item.Result.MemoryUnit = "B";
            }
        }

        var minimumTime = items.Min((p) => p.Result.Value);
        string[] timeUnits = ["µs", "ms", "s"];

        foreach (var unit in timeUnits)
        {
            if (minimumTime < Limit)
            {
                break;
            }

            minimumTime *= Factor;

            foreach (var item in items)
            {
                item.Result.Value *= Factor;
                item.Result.Unit = unit;

                if (item.Result.Range is { Length: > 0 } range)
                {
                    var prefix = range[0..2];
                    var rangeValue = double.Parse(range[2..], CultureInfo.InvariantCulture);
                    rangeValue *= Factor;

                    item.Result.Range = prefix + rangeValue;
                }
            }
        }

        if (items.Where((p) => p is not null).Min((p) => p.Result!.BytesAllocated) is { } minimumMem)
        {
            string[] memoryUnits = ["KB", "MB", "GB", "TB"];

            foreach (var unit in memoryUnits)
            {
                // If the minimum is already less than the limit,
                // we don't need to scale up further.
                if (minimumMem < Limit)
                {
                    break;
                }

                minimumMem *= Factor;

                foreach (var item in items)
                {
                    if (item.Result.BytesAllocated is not null)
                    {
                        item.Result.BytesAllocated *= Factor;
                        item.Result.MemoryUnit = unit;
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var options = Options.Value;

        if ((options.IsGitHubEnterprise || GitHubService.HasToken) &&
            !await GitHubService.VerifyTokenAsync())
        {
            Navigation.NavigateTo(Routes.Token);
            return;
        }

        if (GitHubService.Repositories.Count > 0)
        {
            var repo = GitHubService.Repositories[0];

            if (Repository is { Length: > 0 } name &&
                GitHubService.Repositories.Contains(name, StringComparer.Ordinal))
            {
                repo = name;
            }

            try
            {
                await LoadAsync(() => GitHubService.LoadRepositoryAsync(repo));

                if (Branch is { Length: > 0 } branch &&
                    GitHubService.Branches.Contains(branch, StringComparer.Ordinal))
                {
                    await LoadAsync(() => GitHubService.LoadBenchmarksAsync(branch));
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden && !GitHubService.HasToken)
            {
                // User was rate-limited with anonymous access
                Navigation.NavigateTo(Routes.Token);
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _loading = false;
        }

        if (GitHubService.Benchmarks is { } data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data, AppJsonSerializerContext.Default.BenchmarkResults);
            await JS.InvokeVoidAsync("configureDataDownload", [json, Options.Value.BenchmarkFileName]);
            await JS.InvokeVoidAsync("configureDeepLinks", []);
        }
    }

    [GeneratedRegex(@"^(.*?)(\[(\d+)\])?$", RegexOptions.CultureInvariant)]
    private static partial Regex SuffixRegex();

    private async Task RepositoryChangedAsync(ChangeEventArgs args)
    {
        if (args.Value is string repository)
        {
            await LoadAsync(() => GitHubService.LoadRepositoryAsync(repository));
        }
    }

    private async Task BranchChangedAsync(ChangeEventArgs args)
    {
        if (args.Value is string branch)
        {
            await LoadAsync(() => GitHubService.LoadBenchmarksAsync(branch));
            StateHasChanged();
        }
    }

    private async Task LoadAsync(Func<Task> loader)
    {
        try
        {
            _loading = true;
            await loader();
        }
        finally
        {
            _loading = false;
            _notFound = GitHubService.Benchmarks is null;
        }
    }
}
