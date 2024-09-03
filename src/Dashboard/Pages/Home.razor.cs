// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

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
                if (!sortedGroups.TryGetValue(benchmark.Name, out var results))
                {
                    sortedGroups[benchmark.Name] = results = [];
                }

                results.Add(run.Timestamp, new(run.Commit, benchmark));
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
        const double Limit = 1_000;

        var minimumTime = items.Min((p) => p.Result.Value);
        string[] timeUnits = ["µs", "ms", "s"];

        for (int i = 0; i < timeUnits.Length; i++)
        {
            if (minimumTime < Limit)
            {
                break;
            }

            minimumTime *= Factor;

            for (int j = 0; j < items.Count; j++)
            {
                var item = items[j];

                item.Result.Value *= Factor;
                item.Result.Unit = timeUnits[i];

                if (item.Result.Range is { Length: > 0 } range)
                {
                    var prefix = range[0..2];
                    var rangeValue = double.Parse(range[2..], CultureInfo.InvariantCulture);
                    rangeValue *= Factor;

                    item.Result.Range = prefix + rangeValue;
                }
            }
        }

        if (items.Where((p) => p is not null).Min((p) => p.Result!.BytesAllocated) is { } minimumBytes)
        {
            string[] memoryUnits = ["KB", "MB"];

            for (int i = 0; i < memoryUnits.Length; i++)
            {
                if (minimumBytes < Limit)
                {
                    break;
                }

                minimumBytes *= Factor;

                for (int j = 0; j < items.Count; j++)
                {
                    var item = items[j];

                    if (item.Result.BytesAllocated is not null)
                    {
                        item.Result.BytesAllocated *= Factor;
                        item.Result.MemoryUnit = memoryUnits[i];
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
        }
    }

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
