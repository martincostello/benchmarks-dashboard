// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MartinCostello.Benchmarks.Pages;

public partial class Home : IAsyncDisposable
{
    private const string DefaultMemoryUnit = "bytes";
    private const string EndDateQueryParameter = "endDate";
    private const string QueryDateFormat = "yyyy-MM-dd";
    private const string StartDateQueryParameter = "startDate";

    private DotNetObjectReference<Home>? _dateFilterNavigationReference;
    private BenchmarkResults? _filteredBenchmarks;
    private bool _applyingDateRange;
    private bool _loading = true;
    private DateOnly? _maximumBenchmarkDate;
    private DateOnly? _minimumBenchmarkDate;
    private bool _notFound;
    private bool _resettingDateRange;
    private DateOnly? _selectedEndDate;
    private DateOnly? _selectedStartDate;

    /// <summary>
    /// Gets a value indicating whether to disable the repositories input.
    /// </summary>
    public bool DisableRepositories => GitHubService.Repositories.Count < 1 || ShowLoaders;

    /// <summary>
    /// Gets a value indicating whether to disable the branches input.
    /// </summary>
    public bool DisableBranches => GitHubService.Branches.Count < 1 || ShowLoaders;

    /// <summary>
    /// Gets a value indicating whether to disable the date range inputs.
    /// </summary>
    public bool DisableDateInputs => ShowLoaders || _resettingDateRange || _minimumBenchmarkDate is null || _maximumBenchmarkDate is null;

    /// <summary>
    /// Gets a value indicating whether to disable the date range reset button.
    /// </summary>
    public bool DisableDateReset =>
        _resettingDateRange ||
        DisableDateInputs ||
        (!ShouldPersistDateValue(SelectedStartDateValue, _minimumBenchmarkDate) &&
         !ShouldPersistDateValue(SelectedEndDateValue, _maximumBenchmarkDate));

    /// <summary>
    /// Gets the filtered benchmarks for the selected date range.
    /// </summary>
    public BenchmarkResults FilteredBenchmarks => _filteredBenchmarks ?? new();

    /// <summary>
    /// Gets a value indicating whether benchmark data has an available date range.
    /// </summary>
    public bool HasAvailableBenchmarkDateRange => _minimumBenchmarkDate is not null && _maximumBenchmarkDate is not null;

    /// <summary>
    /// Gets a value indicating whether any benchmarks are available for the selected range.
    /// </summary>
    public bool HasFilteredBenchmarks => _filteredBenchmarks is { Suites.Count: > 0 };

    /// <summary>
    /// Gets the maximum date that can be selected.
    /// </summary>
    public string? MaximumDateValue => FormatDate(_maximumBenchmarkDate);

    /// <summary>
    /// Gets the minimum date that can be selected.
    /// </summary>
    public string? MinimumDateValue => FormatDate(_minimumBenchmarkDate);

    /// <summary>
    /// Gets the currently selected branch, if any.
    /// </summary>
    public string? SelectedBranch =>
        GitHubService.CurrentBranch ??
        Branch ??
        GitHubService.CurrentRepository?.DefaultBranch;

    /// <summary>
    /// Gets the selected end date value.
    /// </summary>
    public string? SelectedEndDateValue => FormatDate(_selectedEndDate);

    /// <summary>
    /// Gets the selected start date value.
    /// </summary>
    public string? SelectedStartDateValue => FormatDate(_selectedStartDate);

    /// <summary>
    /// Gets a value indicating whether to show the loading indicators.
    /// </summary>
    public bool ShowLoaders => _loading || _applyingDateRange;

    /// <summary>
    /// Gets or sets the branch specified by the query string, if any.
    /// </summary>
    [SupplyParameterFromQuery(Name = "branch")]
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the end date specified by the query string, if any.
    /// </summary>
    [SupplyParameterFromQuery(Name = EndDateQueryParameter)]
    public string? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the repository specified by the query string, if any.
    /// </summary>
    [SupplyParameterFromQuery(Name = "repo")]
    public string? Repository { get; set; }

    /// <summary>
    /// Gets or sets the start date specified by the query string, if any.
    /// </summary>
    [SupplyParameterFromQuery(Name = StartDateQueryParameter)]
    public string? StartDate { get; set; }

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

        var uniqueBenchmarks = new HashSet<(string, string)>();

        foreach (var run in runs)
        {
            var timestamp = run.Timestamp;

            foreach (var benchmark in run.Benchmarks)
            {
                var benchmarkForCommit = (run.Commit.Sha, benchmark.Name);

                if (!uniqueBenchmarks.Add(benchmarkForCommit))
                {
                    // We've already seen this commit-name pair, skip it to avoid duplicates.
                    continue;
                }

                var key = benchmark.Name;

                if (!sortedGroups.TryGetValue(key, out var results))
                {
                    sortedGroups[key] = results = [];
                }

                if (!results.ContainsKey(timestamp))
                {
                    results.Add(timestamp, new(run.Commit, benchmark, run.Timestamp));
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
            NormalizeBenchmarkInput(item, hasAllocations);
        }

        var minimumTime = items
            .Where((p) => !double.IsNaN(p.Result.Value))
            .Select((p) => p.Result.Value)
            .DefaultIfEmpty(0)
            .Min();

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

        if (items.Where((p) => p is not null).Min((p) => p.Result.BytesAllocated) is { } minimumMemory)
        {
            string[] memoryUnits = ["KB", "MB", "GB", "TB"];

            foreach (var unit in memoryUnits)
            {
                // If the minimum is already less than the limit,
                // we don't need to scale up further.
                if (minimumMemory < Limit)
                {
                    break;
                }

                minimumMemory *= Factor;

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

    /// <summary>
    /// Applies the specified date range from a chart interaction.
    /// </summary>
    /// <param name="startDate">The start date to apply.</param>
    /// <param name="endDate">The end date to apply.</param>
    /// <param name="hash">The optional hash to apply.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    [JSInvokable]
    public Task ApplyDateRangeFromChartAsync(string? startDate, string? endDate, string? hash)
        => ApplyDateRangeAsync(startDate, endDate, hash);

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _dateFilterNavigationReference?.Dispose();
        _dateFilterNavigationReference = null;
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
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
            var json = System.Text.Json.JsonSerializer.Serialize(_filteredBenchmarks ?? data, AppJsonSerializerContext.Default.BenchmarkResults);
            await JS.InvokeVoidAsync("configureDataDownload", [json, Options.Value.BenchmarkFileName]);
            await JS.InvokeVoidAsync("configureDeepLinks", []);
            _dateFilterNavigationReference ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("configureDateFilterNavigation", [_dateFilterNavigationReference]);
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        _applyingDateRange = false;
        _resettingDateRange = false;

        if (GitHubService.Benchmarks is not null)
        {
            RefreshFilteredBenchmarks();
        }
    }

    private static double NormalizeTimeValue(double value, string? unit) => unit switch
    {
        null or "ns" => value,
        "µs" => value * 1e3,
        "ms" => value * 1e6,
        "s" => value * 1e9,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown time unit."),
    };

    private static double NormalizeMemoryValue(double value, string? unit) => unit switch
    {
        null or DefaultMemoryUnit => value,
        "KB" => value * 1e3,
        "MB" => value * 1e6,
        "GB" => value * 1e9,
        "TB" => value * 1e12,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown memory unit."),
    };

    private static void NormalizeBenchmarkInput(
        BenchmarkItem item,
        bool hasAllocations)
    {
        // This is the expected prefix injection from benchmarkdotnet-results-publisher
        const string RangePrefix = "± ";

        // Normalize to nanoseconds
        item.Result.Value = NormalizeTimeValue(item.Result.Value, item.Result.Unit);

        // Normalize error range
        if (!string.IsNullOrEmpty(item.Result.Range)
            && item.Result.Range.StartsWith(RangePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parsed = double.TryParse(
                item.Result.Range[2..],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var rangeValue);

            if (parsed)
            {
                var updated = NormalizeTimeValue(rangeValue, item.Result.Unit);
                item.Result.Range = $"{RangePrefix}{updated:F}";
            }
        }

        // Set the unit to nanoseconds
        item.Result.Unit = "ns";

        if (item.Result.BytesAllocated is { } allocated)
        {
            item.Result.BytesAllocated = NormalizeMemoryValue(allocated, item.Result.MemoryUnit);
        }

        // I'm not sure how real this will be, but there was an edge case
        // in the tests where 1 entry was missing allocations.
        if (hasAllocations)
        {
            item.Result.MemoryUnit = DefaultMemoryUnit;
        }
    }

    private static BenchmarkResults? FilterBenchmarks(BenchmarkResults? source, DateOnly startDate, DateOnly endDate)
    {
        if (source is null)
        {
            return null;
        }

        Dictionary<string, IList<BenchmarkRun>> filtered = [];

        foreach ((var suite, var runs) in source.Suites)
        {
            var inRange = runs
                .Where((run) =>
                {
                    var date = ToDate(run.Timestamp);
                    return date >= startDate && date <= endDate;
                })
                .ToList();

            if (inRange.Count > 0)
            {
                filtered[suite] = inRange;
            }
        }

        return new()
        {
            LastUpdated = source.LastUpdated,
            RepositoryUrl = source.RepositoryUrl,
            Suites = filtered,
        };
    }

    private static string? FormatDate(DateOnly? value)
        => value?.ToString(QueryDateFormat, CultureInfo.InvariantCulture);

    private static (DateOnly Minimum, DateOnly Maximum)? GetAvailableDateRange(BenchmarkResults? source)
    {
        if (source is null)
        {
            return null;
        }

        DateOnly? minimumDate = null;
        DateOnly? maximumDate = null;

        foreach (var run in source.Suites.Values.SelectMany((runs) => runs))
        {
            var date = ToDate(run.Timestamp);

            if (minimumDate is null || date < minimumDate)
            {
                minimumDate = date;
            }

            if (maximumDate is null || date > maximumDate)
            {
                maximumDate = date;
            }
        }

        return minimumDate is { } minimum && maximumDate is { } maximum ? (minimum, maximum) : null;
    }

    private static bool ShouldPersistDateValue(string? value, DateOnly? boundary)
        => !string.IsNullOrWhiteSpace(value) &&
           boundary is { } date &&
           !string.Equals(value, FormatDate(date), StringComparison.Ordinal);

    private static DateOnly ToDate(DateTimeOffset value)
        => DateOnly.FromDateTime(value.UtcDateTime);

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
            Branch = branch;
            await LoadAsync(() => GitHubService.LoadBenchmarksAsync(branch));
            StateHasChanged();
        }
    }

    private Task EndDateChangedAsync(ChangeEventArgs args)
        => ApplyDateRangeAsync(_selectedStartDate, args.Value as string);

    private async Task ResetDateRangeAsync()
    {
        _resettingDateRange = true;
        StateHasChanged();

        await Task.Yield();
        await ApplyDateRangeAsync(MinimumDateValue, MaximumDateValue);
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
            RefreshFilteredBenchmarks();
        }
    }

    private Task StartDateChangedAsync(ChangeEventArgs args)
        => ApplyDateRangeAsync(args.Value as string, _selectedEndDate);

    private Task ApplyDateRangeAsync(string? startDate, DateOnly? endDate)
        => ApplyDateRangeAsync(startDate, FormatDate(endDate));

    private Task ApplyDateRangeAsync(DateOnly? startDate, string? endDate)
        => ApplyDateRangeAsync(FormatDate(startDate), endDate);

    private Task ApplyDateRangeAsync(string? startDate, string? endDate)
        => ApplyDateRangeAsync(startDate, endDate, hash: null);

    private async Task ApplyDateRangeAsync(string? startDate, string? endDate, string? hash)
    {
        if (_minimumBenchmarkDate is null ||
            _maximumBenchmarkDate is null)
        {
            return;
        }

        startDate = string.IsNullOrWhiteSpace(startDate) ? MinimumDateValue : startDate;
        endDate = string.IsNullOrWhiteSpace(endDate) ? MaximumDateValue : endDate;

        _applyingDateRange = true;
        StateHasChanged();

        await Task.Yield();

        var uri = Navigation.GetUriWithQueryParameters(
            new Dictionary<string, object?>()
            {
                ["branch"] = SelectedBranch,
                [EndDateQueryParameter] = ShouldPersistDateValue(endDate, _maximumBenchmarkDate) ? endDate : null,
                ["repo"] = GitHubService.CurrentRepository?.Name ?? Repository,
                [StartDateQueryParameter] = ShouldPersistDateValue(startDate, _minimumBenchmarkDate) ? startDate : null,
            });

        if (!string.IsNullOrWhiteSpace(hash))
        {
            uri += hash;
        }

        Navigation.NavigateTo(uri);
    }

    private void RefreshFilteredBenchmarks()
    {
        var range = GetAvailableDateRange(GitHubService.Benchmarks);

        if (range is null)
        {
            _filteredBenchmarks = GitHubService.Benchmarks;
            _maximumBenchmarkDate = null;
            _minimumBenchmarkDate = null;
            _selectedEndDate = null;
            _selectedStartDate = null;
            return;
        }

        _minimumBenchmarkDate = range.Value.Minimum;
        _maximumBenchmarkDate = range.Value.Maximum;
        _selectedStartDate = _minimumBenchmarkDate;
        _selectedEndDate = _maximumBenchmarkDate;

        if (TryGetRequestedDateRange(range.Value.Minimum, range.Value.Maximum, out var startDate, out var endDate))
        {
            _selectedStartDate = startDate;
            _selectedEndDate = endDate;
        }

        _filteredBenchmarks = FilterBenchmarks(
            GitHubService.Benchmarks,
            _selectedStartDate.GetValueOrDefault(range.Value.Minimum),
            _selectedEndDate.GetValueOrDefault(range.Value.Maximum));
    }

    private bool TryGetRequestedDateRange(
        DateOnly minimumDate,
        DateOnly maximumDate,
        out DateOnly startDate,
        out DateOnly endDate)
    {
        startDate = minimumDate;
        endDate = maximumDate;

        var hasStartDate = !string.IsNullOrWhiteSpace(StartDate);
        var hasEndDate = !string.IsNullOrWhiteSpace(EndDate);

        if (!hasStartDate && !hasEndDate)
        {
            return false;
        }

        if (hasStartDate &&
            !DateOnly.TryParseExact(StartDate, QueryDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
        {
            startDate = minimumDate;
            endDate = maximumDate;
            return false;
        }

        if (hasEndDate &&
            !DateOnly.TryParseExact(EndDate, QueryDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
        {
            startDate = minimumDate;
            endDate = maximumDate;
            return false;
        }

        if (startDate < minimumDate ||
            endDate > maximumDate ||
            startDate > endDate)
        {
            startDate = minimumDate;
            endDate = maximumDate;
            return false;
        }

        return true;
    }
}
