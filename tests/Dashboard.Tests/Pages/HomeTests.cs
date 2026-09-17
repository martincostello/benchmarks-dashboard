// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;
using Bunit;
using JustEat.HttpClientInterception;
using MartinCostello.Benchmarks.Components;
using MartinCostello.Benchmarks.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace MartinCostello.Benchmarks.Pages;

public class HomeTests : DashboardTestContext
{
    public static TheoryData<double[], double?[], double[], string?, double?[], string?> NormalizationTestCases() =>
        new()
        {
            { [], [], [], null, [], null },
            { [1, 2, 3], [null, null, null], [1, 2, 3], "ns", [null, null, null], null },
            { [1, 2, 3], [1, 2, 3], [1, 2, 3], "ns", [1, 2, 3], "bytes" },
            { [1, 2, 3], [1, null, 3], [1, 2, 3], "ns", [1, null, 3], "bytes" },
            { [100, 1_000, 1_000], [null, null, null], [100, 1_000, 1_000], "ns", [null, null, null], null },
            { [100, 1_000, 1_000], [200, 300, 1_000], [100, 1_000, 1_000], "ns", [200, 300, 1_000], "bytes" },
            { [1_234, 2_345, 3_456], [null, null, null], [1.234, 2.345, 3.456], "µs", [null, null, null], null },
            { [1_234, 2_345, 3_456], [6_789, 7_900, 8_901], [1.234, 2.345, 3.456], "µs", [6.789, 7.900, 8.901], "KB" },
            { [1_234, 2_345, 3_456], [6_789_000, 7_900_000, 8_901_000], [1.234, 2.345, 3.456], "µs", [6.789, 7.900, 8.901], "MB" },
            { [1_234, 2_345, 34_560], [null, null, null], [1.234, 2.345, 34.560], "µs", [null, null, null], null },
            { [123_400, 234_500, 3_456_000], [null, null, null], [123.4, 234.5, 3_456], "µs", [null, null, null], null },
            { [1_234_000, 2_345_000, 3_456_000], [4e9, 8e9, 16e9], [1.234, 2.345, 3.456], "ms", [4, 8, 16], "GB" },
            { [1_234_000_000, 2_345_000_000, 3_456_000_000], [1.5e12, 2.5e12, 5e12], [1.234, 2.345, 3.456], "s", [1.5, 2.5, 5], "TB" },
        };

    [Theory]
    [MemberData(nameof(NormalizationTestCases))]
    public static void NormalizeUnits_Uses_Correct_Units(
        double[] durationValues,
        double?[] memoryValues,
        double[] expectedDurations,
        string? expectedDurationUnits,
        double?[] expectedAllocations,
        string? expectedAllocationUnits)
    {
        // Arrange
        List<BenchmarkItem> items = [];

        for (int i = 0; i < durationValues.Length; i++)
        {
            var result = new BenchmarkResult()
            {
                BytesAllocated = memoryValues[i],
                Value = durationValues[i],
            };

            items.Add(new(new(), result));
        }

        // Act
        Home.NormalizeUnits(items);

        for (int i = 0; i < items.Count; i++)
        {
            var actual = items[i];

            actual.Result.Value.ShouldBe(expectedDurations[i]);
            actual.Result.Unit.ShouldBe(expectedDurationUnits);
            actual.Result.BytesAllocated.ShouldBe(expectedAllocations[i]);
            actual.Result.MemoryUnit.ShouldBe(expectedAllocationUnits);
        }
    }

    [Fact]
    public static void GroupBenchmarks_Groups_Benchmarks_Correctly()
    {
        // Arrange
        var runs = new List<BenchmarkRun>()
        {
            new()
            {
                Timestamp = new DateTimeOffset(2024, 08, 31, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("abc"),
                Benchmarks =
                [
                    new() { Name = "A", Value = 1, Range = "± 0.1" },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("def"),
                Benchmarks =
                [
                    new() { Name = "A", Value = 2 },
                    new() { Name = "B", Value = 2678, Range = "± 17" },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 05, 15, TimeSpan.Zero),
                Commit = CreateCommit("def"),
                Benchmarks =
                [
                    new() { Name = "A", Value = 3 },
                    new() { Name = "B", Value = 2497, Range = "± 14.3" },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 02, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("123"),
                Benchmarks =
                [
                    new() { Name = "A", Value = 4 },
                    new() { Name = "B", Value = 2642, Range = "± 26.7" },
                ],
            },
        };

        // Act
        var actual = Home.GroupBenchmarks(runs);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContainKey("A");
        actual.ShouldContainKey("B");

        var values = actual["A"];

        values.Count.ShouldBe(3);
        values[0].Result.Value.ShouldBe(1);
        values[0].Result.Unit.ShouldBe("ns");
        values[1].Result.Value.ShouldBe(2);
        values[1].Result.Unit.ShouldBe("ns");
        values[2].Result.Value.ShouldBe(4);
        values[2].Result.Unit.ShouldBe("ns");

        values = actual["B"];

        values.Count.ShouldBe(2);
        values[0].Result.Value.ShouldBe(2.678);
        values[0].Result.Unit.ShouldBe("µs");
        values[1].Result.Value.ShouldBe(2.642);
        values[1].Result.Unit.ShouldBe("µs");
    }

    [Fact]
    public static void GroupBenchmarks_Orders_Items_By_Commit_Timestamp_Not_Run_Timestamp()
    {
        // Arrange - the runs are published out of order relative to the commit history.
        // The newer commit's benchmarks finished (and were published) before the older
        // commit's slower benchmarks, so the newer commit has the earlier run timestamp.
        var olderCommit = CreateCommit("older");
        olderCommit.LastUpdated = new DateTimeOffset(2024, 09, 01, 12, 00, 00, TimeSpan.Zero);

        var newerCommit = CreateCommit("newer");
        newerCommit.LastUpdated = new DateTimeOffset(2024, 09, 02, 12, 00, 00, TimeSpan.Zero);

        var runs = new List<BenchmarkRun>()
        {
            new()
            {
                // Newer commit, but its run was published first.
                Timestamp = new DateTimeOffset(2024, 09, 03, 00, 00, 00, TimeSpan.Zero),
                Commit = newerCommit,
                Benchmarks =
                [
                    new() { Name = "A", Value = 2 },
                ],
            },
            new()
            {
                // Older commit, but its run was published later.
                Timestamp = new DateTimeOffset(2024, 09, 04, 00, 00, 00, TimeSpan.Zero),
                Commit = olderCommit,
                Benchmarks =
                [
                    new() { Name = "A", Value = 1 },
                ],
            },
        };

        // Act
        var actual = Home.GroupBenchmarks(runs);

        // Assert - the points are ordered by the commit timestamp (older first), not the
        // run timestamp or the order in which the runs were published.
        var values = actual["A"];

        values.Count.ShouldBe(2);
        values[0].Commit.Sha.ShouldBe("older");
        values[0].Result.Value.ShouldBe(1);
        values[1].Commit.Sha.ShouldBe("newer");
        values[1].Result.Value.ShouldBe(2);
    }

    [Fact]
    public static void GroupBenchmarks_Groups_Benchmarks_Correctly_With_Multiple_Benchmarks()
    {
        // Arrange
        var runs = new List<BenchmarkRun>()
        {
            new()
            {
                Timestamp = new DateTimeOffset(2024, 08, 31, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("abc"),
                Benchmarks =
                [
                    new() { Name = "A", Value = 1, Range = "± 0.1" },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 08, 31, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("abc"),
                Benchmarks =
                [
                    new() { Name = "C", Value = 3, Range = "± 0.3" },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("def"),
                Benchmarks =
                [
                    new() { Name = "A", Value = 2 },
                    new() { Name = "B", Value = 2678, Range = "± 17" },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 05, 00, TimeSpan.Zero),
                Commit = CreateCommit("def"),
                Benchmarks =
                [
                    new() { Name = "C", Value = 4, Range = "± 0.4" },
                ],
            },
        };

        // Act
        var actual = Home.GroupBenchmarks(runs);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContainKey("A");
        actual.ShouldContainKey("B");
        actual.ShouldContainKey("C");

        var values = actual["A"];

        values.Count.ShouldBe(2);
        values[0].Result.Value.ShouldBe(1);
        values[0].Result.Unit.ShouldBe("ns");
        values[1].Result.Value.ShouldBe(2);
        values[1].Result.Unit.ShouldBe("ns");

        values = actual["B"];

        values.Count.ShouldBe(1);
        values[0].Result.Value.ShouldBe(2.678);
        values[0].Result.Unit.ShouldBe("µs");

        values = actual["C"];

        values.Count.ShouldBe(2);
        values[0].Result.Value.ShouldBe(3);
        values[0].Result.Unit.ShouldBe("ns");
        values[1].Result.Value.ShouldBe(4);
        values[1].Result.Unit.ShouldBe("ns");
    }

    [Fact]
    public async Task Page_Renders()
    {
        // Arrange
        string repository = "benchmarks-demo";

        await WithValidAccessToken();

        WithBenchmarks(repository, "main");

        SetupJSInterop();

        // Act
        var actual = Render<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("[name='repo']").ShouldNotBeNull();
                actual.Find("[name='branch']").ShouldNotBeNull();
                actual.Find("[name='startDate']").ShouldNotBeNull();
                actual.Find("[name='endDate']").ShouldNotBeNull();
                actual.Find("[id='branch']").ShouldNotBeNull();
                actual.FindAll(".benchmark-set").Count.ShouldBe(4);
                actual.FindAll(".benchmark-chart").Count.ShouldBe(9);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Uses_Available_Date_Range_By_Default()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";

        await WithValidAccessToken();

        WithBenchmarks(Repository, Branch);

        SetupJSInterop();

        (var expectedStart, var expectedEnd) = GetAvailableDateRange($"{Repository}-{Branch}");

        // Act
        var actual = Render<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                var start = actual.Find("#startDate");
                var end = actual.Find("#endDate");

                start.GetAttribute("value").ShouldBe(expectedStart);
                start.GetAttribute("min").ShouldBe(expectedStart);
                start.GetAttribute("max").ShouldBe(expectedEnd);

                end.GetAttribute("value").ShouldBe(expectedEnd);
                end.GetAttribute("min").ShouldBe(expectedStart);
                end.GetAttribute("max").ShouldBe(expectedEnd);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Applies_Valid_Date_Filter_From_Query_String()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";
        const string StartDate = "2024-08-21";
        const string EndDate = "2024-08-22";
        const string BenchmarkName = "DotNetBenchmarks.TodoAppBenchmarks.GetOneTodo";
        const string SuiteName = "DotNetBenchmarks.TodoAppBenchmarks";

        await WithValidAccessToken();

        WithBenchmarks(Repository, Branch);

        SetupJSInterop();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"?repo={Repository}&branch={Branch}&startDate={StartDate}&endDate={EndDate}");

        var data = LoadBenchmarkResults($"{Repository}-{Branch}");

        var start = DateOnly.Parse(StartDate, CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue);
        var end = DateOnly.Parse(EndDate, CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue);

        var filtered = data.Suites[SuiteName]
            .Where((run) => run.Timestamp.UtcDateTime.Date >= start && run.Timestamp.UtcDateTime.Date <= end)
            .ToList();

        var expectedCount = Home.GroupBenchmarks(filtered)[BenchmarkName].Count;

        // Act
        var actual = Render<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("#startDate").GetAttribute("value").ShouldBe(StartDate);
                actual.Find("#endDate").GetAttribute("value").ShouldBe(EndDate);

                var benchmark = actual.FindComponents<Benchmark>()
                    .Single((item) =>
                        item.Instance.Name == BenchmarkName &&
                        item.Instance.Suite == SuiteName);

                benchmark.Instance.Items.Count.ShouldBe(expectedCount);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Applies_Same_Day_Date_Filter_As_Exact_24_Hour_Period()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";
        const string SelectedDate = "2024-08-22";
        const string SuiteName = "SameDayBenchmarks";
        const string BenchmarkName = "SameDayBenchmarks.ExactDay";

        await WithValidAccessToken();

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}", $"{Repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}/branches", $"{Repository}-branches");

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{Repository}/data.json?ref={Branch}")
            .WithJsonContent(new BenchmarkResults()
            {
                LastUpdated = DateTimeOffset.UtcNow,
                RepositoryUrl = $"https://github.local/{Options.RepositoryOwner}/{Repository}",
                Suites = new Dictionary<string, IList<BenchmarkRun>>()
                {
                    [SuiteName] =
                    [
                        new()
                        {
                            Commit = CreateCommit("aaaaaaa1"),
                            Timestamp = new DateTimeOffset(2024, 08, 22, 00, 00, 00, TimeSpan.Zero),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 1, Unit = "ns" }],
                        },
                        new()
                        {
                            Commit = CreateCommit("bbbbbbb2"),
                            Timestamp = new DateTimeOffset(2024, 08, 22, 23, 59, 59, TimeSpan.Zero),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 2, Unit = "ns" }],
                        },
                        new()
                        {
                            Commit = CreateCommit("ccccccc3"),
                            Timestamp = new DateTimeOffset(2024, 08, 21, 23, 59, 59, TimeSpan.Zero),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 3, Unit = "ns" }],
                        },
                        new()
                        {
                            Commit = CreateCommit("ddddddd4"),
                            Timestamp = new DateTimeOffset(2024, 08, 23, 00, 00, 00, TimeSpan.Zero),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 4, Unit = "ns" }],
                        },
                    ],
                },
            });

        builder.RegisterWith(Interceptor);

        SetupJSInterop();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"?repo={Repository}&branch={Branch}&startDate={SelectedDate}&endDate={SelectedDate}");

        // Act
        var actual = Render<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("#startDate").GetAttribute("value").ShouldBe(SelectedDate);
                actual.Find("#endDate").GetAttribute("value").ShouldBe(SelectedDate);

                var benchmark = actual.FindComponents<Benchmark>()
                    .Single((item) =>
                        item.Instance.Name == BenchmarkName &&
                        item.Instance.Suite == SuiteName);

                benchmark.Instance.Items.Count.ShouldBe(2);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Filters_Benchmarks_By_Selected_Environment_Metadata()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";
        const string SuiteName = "EnvironmentBenchmarks";
        const string BenchmarkName = "EnvironmentBenchmarks.Method";
        const string AmdProcessorA = "AMD EPYC 9V74";
        const string AmdProcessorB = "AMD EPYC 9754";
        const string IntelProcessor = "Intel Xeon 6973P-C";

        await WithValidAccessToken();

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}", $"{Repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}/branches", $"{Repository}-branches");

        static BenchmarkMetadata CreateMetadata(string processorName) =>
            new()
            {
                Environment = new Dictionary<string, JsonElement>()
                {
                    ["ProcessorName"] = JsonSerializer.SerializeToElement(processorName),
                    ["Architecture"] = JsonSerializer.SerializeToElement("X64"),
                },
            };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{Repository}/data.json?ref={Branch}")
            .WithJsonContent(new BenchmarkResults()
            {
                LastUpdated = DateTimeOffset.UtcNow,
                RepositoryUrl = $"https://github.local/{Options.RepositoryOwner}/{Repository}",
                Suites = new Dictionary<string, IList<BenchmarkRun>>()
                {
                    [SuiteName] =
                    [
                        new()
                        {
                            Commit = CreateCommit("aaaaaaa1"),
                            Timestamp = new DateTimeOffset(2024, 08, 20, 00, 00, 00, TimeSpan.Zero),
                            Metadata = CreateMetadata(AmdProcessorA),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 1, Unit = "ns" }],
                        },
                        new()
                        {
                            Commit = CreateCommit("bbbbbbb2"),
                            Timestamp = new DateTimeOffset(2024, 08, 21, 00, 00, 00, TimeSpan.Zero),
                            Metadata = CreateMetadata(IntelProcessor),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 2, Unit = "ns" }],
                        },
                        new()
                        {
                            Commit = CreateCommit("ccccccc3"),
                            Timestamp = new DateTimeOffset(2024, 08, 22, 00, 00, 00, TimeSpan.Zero),
                            Metadata = CreateMetadata(AmdProcessorB),
                            Benchmarks = [new() { Name = BenchmarkName, Value = 3, Unit = "ns" }],
                        },
                    ],
                },
            });

        builder.RegisterWith(Interceptor);

        SetupJSInterop();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"?repo={Repository}&branch={Branch}");

        // Act
        var actual = Render<Home>();

        // Assert - "Architecture" is the same for every run so it should not be
        // selectable, but "ProcessorName" has distinct values so it should be.
        actual.WaitForAssertion(
            () =>
            {
                actual.FindAll("#env-filter-Architecture").Count.ShouldBe(0);
                actual.Find("#env-filter-ProcessorName").ShouldNotBeNull();

                var benchmark = actual.FindComponents<Benchmark>()
                    .Single((item) => item.Instance.Name == BenchmarkName && item.Instance.Suite == SuiteName);

                benchmark.Instance.Items.Count.ShouldBe(3);
            },
            TimeSpan.FromSeconds(2));

        // Act - multi-select both AMD processor variants at once
        await actual.Find("#env-filter-ProcessorName")
            .TriggerEventAsync(
                "onchange",
                new ChangeEventArgs() { Value = new[] { $"\"{AmdProcessorA}\"", $"\"{AmdProcessorB}\"" } });

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                var benchmark = actual.FindComponents<Benchmark>()
                    .Single((item) => item.Instance.Name == BenchmarkName && item.Instance.Suite == SuiteName);

                benchmark.Instance.Items.Count.ShouldBe(2);
                benchmark.Instance.Items.ShouldAllBe(
                    (item) => item.Metadata!.Environment!["ProcessorName"].GetString() == AmdProcessorA ||
                              item.Metadata!.Environment!["ProcessorName"].GetString() == AmdProcessorB);
            },
            TimeSpan.FromSeconds(2));

        // Act - selecting "All" alongside other values is mutually exclusive: it wins over any other selection
        await actual.Find("#env-filter-ProcessorName")
            .TriggerEventAsync(
                "onchange",
                new ChangeEventArgs() { Value = new[] { string.Empty, $"\"{AmdProcessorA}\"" } });

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                var benchmark = actual.FindComponents<Benchmark>()
                    .Single((item) => item.Instance.Name == BenchmarkName && item.Instance.Suite == SuiteName);

                benchmark.Instance.Items.Count.ShouldBe(3);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Shows_Loading_Spinner_While_Environment_Filter_Changes()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";
        const string SuiteName = "EnvironmentBenchmarks";
        const string AmdProcessor = "AMD EPYC 9V74";
        const string IntelProcessor = "Intel Xeon 6973P-C";

        await WithValidAccessToken();

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}", $"{Repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}/branches", $"{Repository}-branches");

        static BenchmarkMetadata CreateMetadata(string processorName) =>
            new()
            {
                Environment = new Dictionary<string, JsonElement>()
                {
                    ["ProcessorName"] = JsonSerializer.SerializeToElement(processorName),
                },
            };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{Repository}/data.json?ref={Branch}")
            .WithJsonContent(new BenchmarkResults()
            {
                LastUpdated = DateTimeOffset.UtcNow,
                RepositoryUrl = $"https://github.local/{Options.RepositoryOwner}/{Repository}",
                Suites = new Dictionary<string, IList<BenchmarkRun>>()
                {
                    [SuiteName] =
                    [
                        new()
                        {
                            Commit = CreateCommit("aaaaaaa1"),
                            Timestamp = new DateTimeOffset(2024, 08, 20, 00, 00, 00, TimeSpan.Zero),
                            Metadata = CreateMetadata(AmdProcessor),
                            Benchmarks = [new() { Name = "EnvironmentBenchmarks.Method", Value = 1, Unit = "ns" }],
                        },
                        new()
                        {
                            Commit = CreateCommit("bbbbbbb2"),
                            Timestamp = new DateTimeOffset(2024, 08, 21, 00, 00, 00, TimeSpan.Zero),
                            Metadata = CreateMetadata(IntelProcessor),
                            Benchmarks = [new() { Name = "EnvironmentBenchmarks.Method", Value = 2, Unit = "ns" }],
                        },
                    ],
                },
            });

        builder.RegisterWith(Interceptor);

        SetupJSInterop();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"?repo={Repository}&branch={Branch}");

        var actual = Render<Home>();

        actual.WaitForAssertion(
            () => actual.FindAll("#benchmarks").Count.ShouldBe(1),
            TimeSpan.FromSeconds(2));

        // Capture a snapshot of the rendered markup after every render so that the
        // transient "applying the filter" render can be inspected once settled, even
        // though it may have already been superseded by the time we can assert on it.
        List<string> renders = [];
        actual.OnAfterRender += (_, _) => renders.Add(actual.Markup);

        // Act
        await actual
            .Find("#env-filter-ProcessorName")
            .TriggerEventAsync("onchange", new ChangeEventArgs() { Value = new[] { $"\"{AmdProcessor}\"" } });

        // Assert - a render occurred where the spinner was shown and the charts were hidden
        // while the filter was being applied, and the final state has the filtered charts shown.
        renders.ShouldContain(
            (markup) => markup.Contains("id=\"date-range-loader\"", StringComparison.Ordinal) &&
                        !markup.Contains("date-range-loader d-none", StringComparison.Ordinal) &&
                        !markup.Contains("id=\"benchmarks\"", StringComparison.Ordinal));

        actual.FindAll("#benchmarks").Count.ShouldBe(1);
        actual.Find("#date-range-loader").ClassList.ShouldContain("d-none");
    }

    [Fact]
    public async Task Page_Treats_Invalid_Date_Filter_As_Absent()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";

        await WithValidAccessToken();

        WithBenchmarks(Repository, Branch);

        SetupJSInterop();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"?repo={Repository}&branch={Branch}&startDate=2024-08-21&endDate=not-a-date");

        var (expectedStart, expectedEnd) = GetAvailableDateRange($"{Repository}-{Branch}");

        // Act
        var actual = Render<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("#startDate").GetAttribute("value").ShouldBe(expectedStart);
                actual.Find("#endDate").GetAttribute("value").ShouldBe(expectedEnd);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Can_Reset_Date_Filter()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";

        await WithValidAccessToken();

        WithBenchmarks(Repository, Branch);

        SetupJSInterop();

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"?repo={Repository}&branch={Branch}&startDate=2024-08-21&endDate=2024-08-22");

        var (expectedStart, expectedEnd) = GetAvailableDateRange($"{Repository}-{Branch}");

        // Act
        var actual = Render<Home>();

        actual.WaitForAssertion(
            () =>
            {
                var reset = actual.Find("#resetDateRange");
                reset.HasAttribute("disabled").ShouldBeFalse();
                reset.GetAttribute("aria-busy").ShouldBe("false");
                reset.ClassList.ShouldContain("btn-secondary");
                reset.ClassList.ShouldNotContain("btn-outline-secondary");
            },
            TimeSpan.FromSeconds(10));

        var click = actual.Find("#resetDateRange").ClickAsync(new());

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                navigation.Uri.ShouldContain($"repo={Repository}");
                navigation.Uri.ShouldContain($"branch={Branch}");
                navigation.Uri.ShouldNotContain("startDate=");
                navigation.Uri.ShouldNotContain("endDate=");
                actual.Find("#startDate").GetAttribute("value").ShouldBe(expectedStart);
                actual.Find("#endDate").GetAttribute("value").ShouldBe(expectedEnd);
                var reset = actual.Find("#resetDateRange");
                reset.HasAttribute("disabled").ShouldBeTrue();
                reset.GetAttribute("aria-busy").ShouldBe("false");
                reset.TextContent.ShouldContain("Reset date range");
            },
            TimeSpan.FromSeconds(10));

        await click;
    }

    [Fact]
    public async Task Page_Hides_Charts_While_Date_Filter_Changes()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";
        const string StartDate = "2024-08-22";

        await WithValidAccessToken();

        WithBenchmarks(Repository, Branch);

        SetupJSInterop();

        var navigation = Services.GetRequiredService<NavigationManager>();
        var actual = Render<Home>();

        actual.WaitForAssertion(
            () => actual.FindAll("#benchmarks").Count.ShouldBe(1),
            TimeSpan.FromSeconds(2));

        // Act
        var change = actual.Find("#startDate")
            .TriggerEventAsync("onchange", new ChangeEventArgs() { Value = StartDate });

        // Assert
        actual.WaitForAssertion(
            () => navigation.Uri.ShouldContain($"startDate={StartDate}"),
            TimeSpan.FromSeconds(10));

        await change;
    }

    [Fact]
    public async Task Page_Updates_Date_Filter_When_Query_String_Changes()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";
        const string StartDate = "2024-08-21";
        const string EndDate = "2024-08-22";

        await WithValidAccessToken();

        WithBenchmarks(Repository, Branch);

        SetupJSInterop();

        var actual = Render<Home>();

        // Act
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"?repo={Repository}&branch={Branch}&startDate={StartDate}&endDate={EndDate}");

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("#startDate").GetAttribute("value").ShouldBe(StartDate);
                actual.Find("#endDate").GetAttribute("value").ShouldBe(EndDate);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Page_Uses_Current_Branch_For_Date_Filter_After_Branch_Change()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string InitialBranch = "dotnet-nightly";
        const string UpdatedBranch = "main";
        const string InitialStartDate = "2024-08-21";
        const string InitialEndDate = "2024-08-22";
        const string UpdatedStartDate = "2024-08-22";

        await WithValidAccessToken();

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}", $"{Repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}/branches", $"{Repository}-branches");
        RegisterResponse(
            $"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{Repository}/data.json?ref={InitialBranch}",
            $"{Repository}-main");
        RegisterResponse(
            $"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{Repository}/data.json?ref={UpdatedBranch}",
            $"{Repository}-main");

        SetupJSInterop();

        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"?repo={Repository}&branch={InitialBranch}&startDate={InitialStartDate}&endDate={InitialEndDate}");

        var actual = Render<Home>();

        actual.WaitForAssertion(
            () => Services.GetRequiredService<GitHubService>().CurrentBranch.ShouldBe(InitialBranch),
            TimeSpan.FromSeconds(2));

        // Act
        await actual.Find("#branch").TriggerEventAsync("onchange", new ChangeEventArgs() { Value = UpdatedBranch });

        actual.WaitForAssertion(
            () => Services.GetRequiredService<GitHubService>().CurrentBranch.ShouldBe(UpdatedBranch),
            TimeSpan.FromSeconds(2));

        await actual.Find("#startDate").TriggerEventAsync("onchange", new ChangeEventArgs() { Value = UpdatedStartDate });

        // Assert
        navigation.Uri.ShouldContain($"repo={Repository}");
        navigation.Uri.ShouldContain($"branch={UpdatedBranch}");
        navigation.Uri.ShouldNotContain($"branch={InitialBranch}");
        navigation.Uri.ShouldContain($"startDate={UpdatedStartDate}");
    }

    [Fact]
    public async Task Page_Shows_Empty_Data_Message_When_No_Benchmarks_Are_Available()
    {
        // Arrange
        const string Repository = "benchmarks-demo";
        const string Branch = "main";

        await WithValidAccessToken();

        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}", $"{Repository}-repo");
        RegisterResponse($"https://api.github.local/repos/{Options.RepositoryOwner}/{Repository}/branches", $"{Repository}-branches");

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl($"https://api.github.local/repos/{Options.RepositoryOwner}/{Options.RepositoryName}/contents/{Repository}/data.json?ref={Branch}")
            .WithJsonContent(new BenchmarkResults()
            {
                LastUpdated = DateTimeOffset.UtcNow,
                RepositoryUrl = "https://github.local/martincostello/benchmarks-demo",
                Suites = new Dictionary<string, IList<BenchmarkRun>>(),
            });

        builder.RegisterWith(Interceptor);

        SetupJSInterop();

        // Act
        var actual = Render<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("#no-benchmarks-available").TextContent.ShouldContain("No benchmark data is available.");
                actual.FindAll("#no-benchmarks-in-range").Count.ShouldBe(0);
            },
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void NormalizeUnits_Handles_Mixed_Memory_Units()
    {
        // Arrange: three items, memory in KB, MB, and null
        var items = new List<BenchmarkItem>
        {
            new(new(), new BenchmarkResult { Value = 1, Unit = "ns", BytesAllocated = 300, MemoryUnit = "KB" }),
            new(new(), new BenchmarkResult { Value = 1, Unit = "ns", BytesAllocated = 1, MemoryUnit = "MB" }),
            new(new(), new BenchmarkResult { Value = 1, Unit = "ns", BytesAllocated = null, MemoryUnit = null }),
        };

        // Act
        Home.NormalizeUnits(items);

        // Assert: everything scales to KB because minimum is 300 KB
        items[0].Result.BytesAllocated.ShouldBe(300);
        items[0].Result.MemoryUnit.ShouldBe("KB");

        items[1].Result.BytesAllocated.ShouldBe(1_000);
        items[1].Result.MemoryUnit.ShouldBe("KB");

        // Null remains null, but MemoryUnit is set to the base unit "B" (bytes) during normalization
        items[2].Result.BytesAllocated.ShouldBeNull();
        items[2].Result.MemoryUnit.ShouldBe("bytes");

        // Time stays as ns as inputs were small
        items.ForEach(i => i.Result.Unit.ShouldBe("ns"));
    }

    [Fact]
    public void NormalizeUnits_Converts_Range_With_Time_Units()
    {
        // Arrange: values in ms with a range; should normalize to ns and back to ms
        var items = new List<BenchmarkItem>
        {
            new(new(), new BenchmarkResult { Value = 1.5, Unit = "ms", Range = "± 0.1" }),
            new(new(), new BenchmarkResult { Value = 0.8, Unit = "ms" }),
        };

        // Act
        Home.NormalizeUnits(items);

        // Assert
        var first = items[0].Result;
        var second = items[1].Result;

        first.Value.ShouldBe(1.5);
        first.Unit.ShouldBe("ms");
        first.Range.ShouldBe("± 0.1");

        second.Value.ShouldBe(0.8);
        second.Unit.ShouldBe("ms");
        second.Range.ShouldBeNull();
    }

    [Fact]
    public void GroupBenchmarks_Appends_Suffix_For_Duplicate_Timestamps()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 09, 03, 12, 00, 00, TimeSpan.Zero);
        var run = new BenchmarkRun
        {
            Timestamp = timestamp,
            Commit = CreateCommit("duplicate"),
            Benchmarks =
            [
                new() { Name = "Unique", Value = 1 },
                new() { Name = "Duplicate", Value = 10 },
                new() { Name = "Duplicate", Value = 20 },
                new() { Name = "Duplicate", Value = 30 },
                new() { Name = "Duplicate[Job]", Value = 40 },
                new() { Name = "Duplicate[Job]", Value = 50 },
                new() { Name = "Duplicate[Job]", Value = 60 },
            ],
        };

        // Act
        var grouped = Home.GroupBenchmarks([run]);

        // Assert
        grouped.Keys.ShouldBe(
        [
            "Unique",
            "Duplicate",
            "Duplicate[Job]",
        ]);

        grouped["Unique"].Single().Result.Value.ShouldBe(1);
        grouped["Duplicate"].Single().Result.Value.ShouldBe(10);
        grouped["Duplicate[Job]"].Single().Result.Value.ShouldBe(40);
    }

    [Fact]
    public void GroupBenchmarks_Copies_Environment_Metadata_From_The_Run()
    {
        // Arrange
        var metadata = new BenchmarkMetadata()
        {
            Environment = new Dictionary<string, JsonElement>()
            {
                ["ProcessorName"] = JsonSerializer.SerializeToElement("AMD Ryzen 9 5950X"),
                ["LogicalCoreCount"] = JsonSerializer.SerializeToElement(32),
            },
        };

        var runWithMetadata = new BenchmarkRun()
        {
            Timestamp = new DateTimeOffset(2024, 09, 03, 12, 00, 00, TimeSpan.Zero),
            Commit = CreateCommit("with-metadata"),
            Metadata = metadata,
            Benchmarks = [new() { Name = "A", Value = 1 }],
        };

        var runWithoutMetadata = new BenchmarkRun()
        {
            Timestamp = new DateTimeOffset(2024, 09, 04, 12, 00, 00, TimeSpan.Zero),
            Commit = CreateCommit("without-metadata"),
            Benchmarks = [new() { Name = "A", Value = 2 }],
        };

        // Act
        var grouped = Home.GroupBenchmarks([runWithMetadata, runWithoutMetadata]);

        // Assert
        var items = grouped["A"];

        items[0].Metadata.ShouldBe(metadata);
        items[1].Metadata.ShouldBeNull();
    }

    [Fact]
    public void GetEnvironmentFilterOptions_Returns_Empty_List_For_Null_Source()
    {
        // Act
        var actual = Home.GetEnvironmentFilterOptions(null);

        // Assert
        actual.ShouldBeEmpty();
    }

    [Fact]
    public void GetEnvironmentFilterOptions_Excludes_Metadata_With_A_Single_Distinct_Value()
    {
        // Arrange
        var results = CreateBenchmarkResultsWithEnvironmentMetadata(
            [
                new() { ["ProcessorName"] = "AMD EPYC 9V74", ["Architecture"] = "X64" },
                new() { ["ProcessorName"] = "Intel Xeon 6973P-C", ["Architecture"] = "X64" },
                new() { ["ProcessorName"] = "AMD EPYC 9V74" }, // No Architecture value at all
            ]);

        // Act
        var actual = Home.GetEnvironmentFilterOptions(results);

        // Assert
        actual.Select((option) => option.Key).ShouldBe(["ProcessorName"]);

        var processorName = actual.Single();

        processorName.DisplayName.ShouldBe("Processor");
        processorName.Values.Select((value) => value.DisplayValue).ShouldBe(
            ["AMD EPYC 9V74", "Intel Xeon 6973P-C"],
            ignoreOrder: true);
    }

    [Theory]
    [InlineData("DotNetCliVersion", ".NET SDK")]
    [InlineData("OsVersion", "OS")]
    [InlineData("ProcessorName", "Processor")]
    [InlineData("RuntimeVersion", ".NET Runtime")]
    [InlineData("LogicalCoreCount", "LogicalCoreCount")]
    public void GetEnvironmentFilterOptions_Uses_Friendly_Display_Names(string key, string expectedDisplayName)
    {
        // Arrange
        var results = CreateBenchmarkResultsWithEnvironmentMetadata(
            [
                new() { [key] = "first" },
                new() { [key] = "second" },
            ]);

        // Act
        var actual = Home.GetEnvironmentFilterOptions(results);

        // Assert
        actual.Single().DisplayName.ShouldBe(expectedDisplayName);
    }

    [Fact]
    public void GetEnvironmentFilterOptions_Sorts_By_Display_Name_Not_Key()
    {
        // Arrange
        var results = CreateBenchmarkResultsWithEnvironmentMetadata(
            [
                new() { ["ProcessorName"] = "AMD", ["RuntimeVersion"] = "8.0.0" },
                new() { ["ProcessorName"] = "Intel", ["RuntimeVersion"] = "9.0.0" },
            ]);

        // Act
        var actual = Home.GetEnvironmentFilterOptions(results);

        // Assert
        actual.Select((option) => option.DisplayName).ShouldBe([".NET Runtime", "Processor"]);
    }

    [Fact]
    public void GetEnvironmentFilterOptions_Ignores_Runs_Without_Metadata()
    {
        // Arrange
        var results = new BenchmarkResults()
        {
            Suites = new Dictionary<string, IList<BenchmarkRun>>()
            {
                ["Suite"] =
                [
                    new()
                    {
                        Commit = CreateCommit("no-metadata"),
                        Timestamp = DateTimeOffset.UtcNow,
                        Benchmarks = [new() { Name = "A", Value = 1 }],
                    },
                ],
            },
        };

        // Act
        var actual = Home.GetEnvironmentFilterOptions(results);

        // Assert
        actual.ShouldBeEmpty();
    }

    [Fact]
    public void NormalizeUnits_Scales_From_Nanoseconds_To_Microseconds_And_Updates_Range()
    {
        // Arrange
        var items = new List<BenchmarkItem>
        {
            new(new(), new BenchmarkResult { Value = 800, Unit = "ns", Range = "± 0.1" }),
            new(new(), new BenchmarkResult { Value = 900, Unit = "ns", Range = "± 0.2" }),
        };

        // Act
        Home.NormalizeUnits(items);

        // Assert
        var first = items[0].Result;
        var second = items[1].Result;

        first.Value.ShouldBe(0.8);
        first.Unit.ShouldBe("µs");
        first.Range.ShouldBe("± 0.0001");

        second.Value.ShouldBe(0.9);
        second.Unit.ShouldBe("µs");
        second.Range.ShouldBe("± 0.0002");
    }

    [Fact]
    public void NormalizeUnits_Scales_Large_Memory_Units_To_Gigabytes()
    {
        // Arrange
        var items = new List<BenchmarkItem>
        {
            new(new(), new BenchmarkResult { Value = 1, Unit = "ns", BytesAllocated = 1, MemoryUnit = "GB" }),
            new(new(), new BenchmarkResult { Value = 1, Unit = "ns", BytesAllocated = 1, MemoryUnit = "TB" }),
        };

        // Act
        Home.NormalizeUnits(items);

        // Assert
        var first = items[0].Result;
        var second = items[1].Result;

        first.BytesAllocated.ShouldBe(1);
        first.MemoryUnit.ShouldBe("GB");

        second.BytesAllocated.ShouldBe(1_000);
        second.MemoryUnit.ShouldBe("GB");
    }

    [Fact]
    public void NormalizeUnits_Throws_On_Unknown_Time_Unit()
    {
        // Arrange
        var items = new List<BenchmarkItem> { new(new(), new BenchmarkResult { Value = 1, Unit = "years" }), };

        // Act and Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Home.NormalizeUnits(items));
        exception.ActualValue.ShouldBe("years");
    }

    [Fact]
    public void NormalizeUnits_Throws_On_Unknown_Memory_Unit()
    {
        // Arrange
        var items = new List<BenchmarkItem>
        {
            new(new(), new BenchmarkResult { Value = 1, Unit = "ns", BytesAllocated = 1, MemoryUnit = "PB" }),
        };

        // Act and Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() => Home.NormalizeUnits(items));
        exception.ActualValue.ShouldBe("PB");
    }

    [Fact]
    public void NormalizeUnits_Handles_All_Memory_Units()
    {
        // Arrange
        string[] units = ["bytes", "KB", "MB", "GB", "TB"];
        List<BenchmarkItem> items = [];

        foreach (var unit in units)
        {
            items.Add(new(new(), new() { Value = 1, Unit = "ns", BytesAllocated = 1, MemoryUnit = unit }));
        }

        // Act
        Home.NormalizeUnits(items);

        // Assert
        double value = 1;

        foreach (var item in items)
        {
            item.Result.BytesAllocated.ShouldBe(value);
            item.Result.MemoryUnit.ShouldBe("bytes");

            value *= 1_000;
        }
    }

    [Fact]
    public void NormalizeUnits_Handles_All_Time_Units()
    {
        // Arrange
        string[] units = ["ns", "µs", "ms", "s"];
        List<BenchmarkItem> items = [];

        foreach (var unit in units)
        {
            items.Add(new(new(), new() { Value = 1, Unit = unit }));
        }

        // Act
        Home.NormalizeUnits(items);

        // Assert
        double value = 1;

        foreach (var item in items)
        {
            item.Result.Value.ShouldBe(value);
            item.Result.Unit.ShouldBe("ns");

            value *= 1_000;
        }
    }

    [Fact]
    public void NormalizeUnits_Handles_Missing_Values()
    {
        // Arrange
        List<BenchmarkItem> items =
        [
            new(new(), new() { Value = 124367.601, Unit = "ns" }),
            new(new(), new() { Value = double.NaN, Unit = "ns" }),
            new(new(), new() { Value = 336148.104, Unit = "ns" }),
        ];

        // Act
        Home.NormalizeUnits(items);

        // Assert
        items[0].Result.Value.ShouldBe(124.367601);
        items[0].Result.Unit.ShouldBe("µs");

        items[1].Result.Value.ShouldBe(double.NaN);
        items[1].Result.Unit.ShouldBe("µs");

        items[2].Result.Value.ShouldBe(336.148104);
        items[2].Result.Unit.ShouldBe("µs");
    }

    [Fact]
    public void NormalizeUnits_Handles_All_Missing_Values()
    {
        // Arrange
        List<BenchmarkItem> items =
        [
            new(new(), new() { Value = double.NaN, Unit = "ns" }),
            new(new(), new() { Value = double.NaN, Unit = "ns" }),
        ];

        // Act
        Home.NormalizeUnits(items);

        // Assert
        items[0].Result.Value.ShouldBe(double.NaN);
        items[0].Result.Unit.ShouldBe("ns");

        items[1].Result.Value.ShouldBe(double.NaN);
        items[1].Result.Unit.ShouldBe("ns");
    }

    [Fact]
    public void NormalizeUnits_Handles_No_Values()
    {
        // Arrange
        List<BenchmarkItem> items = [];

        // Act and Assert
        Should.NotThrow(() => Home.NormalizeUnits(items));
    }

    private static BenchmarkResults CreateBenchmarkResultsWithEnvironmentMetadata(
        IReadOnlyList<Dictionary<string, string>> environmentValuesByRun)
    {
        List<BenchmarkRun> runs = [];

        for (var i = 0; i < environmentValuesByRun.Count; i++)
        {
            var environment = environmentValuesByRun[i]
                .ToDictionary(
                    (p) => p.Key,
                    (p) => JsonSerializer.SerializeToElement(p.Value));

            runs.Add(new()
            {
                Commit = CreateCommit($"commit-{i}"),
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 00, 00, TimeSpan.Zero).AddDays(i),
                Metadata = new() { Environment = environment },
                Benchmarks = [new() { Name = "A", Value = i }],
            });
        }

        return new()
        {
            Suites = new Dictionary<string, IList<BenchmarkRun>>()
            {
                ["Suite"] = runs,
            },
        };
    }

    private static GitCommit CreateCommit(string sha) =>
        new()
        {
            Author = new() { UserName = "octocat" },
            Committer = new() { UserName = "webflow" },
            LastUpdated = DateTime.UtcNow,
            Message = "Update code",
            Sha = sha,
            Url = $"https://github.local/octocat/repository/commits/{sha}",
        };

    private static (string StartDate, string EndDate) GetAvailableDateRange(string responseName)
    {
        var data = LoadBenchmarkResults(responseName);

        var dates = data.Suites
            .Values
            .SelectMany((runs) => runs)
            .Select((run) => DateOnly.FromDateTime(run.Timestamp.UtcDateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .OrderBy((value) => value, StringComparer.Ordinal)
            .ToList();

        return (dates[0], dates[^1]);
    }

    private static BenchmarkResults LoadBenchmarkResults(string responseName)
    {
        using var stream = File.OpenRead(Path.Combine(".", "Responses", $"{responseName}.json"));
        return JsonSerializer.Deserialize(stream, AppJsonSerializerContext.Default.BenchmarkResults)!;
    }

    private void SetupJSInterop()
    {
        JSInterop.SetupVoid("configureDataDownload", static (_) => true).SetVoidResult();
        JSInterop.SetupVoid("configureDateFilterNavigation", static (_) => true).SetVoidResult();
        JSInterop.SetupVoid("configureDeepLinks", static (_) => true).SetVoidResult();
        JSInterop.SetupVoid("renderChart", static (_) => true).SetVoidResult();
        JSInterop.SetupVoid("scrollToActiveChart").SetVoidResult();
    }
}
