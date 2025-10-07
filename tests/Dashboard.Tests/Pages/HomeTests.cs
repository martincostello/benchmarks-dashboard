// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;
using MartinCostello.Benchmarks.Models;

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
    public async Task Page_Renders()
    {
        // Arrange
        string repository = "benchmarks-demo";

        await WithValidAccessToken();

        WithBenchmarks(repository, "main");

        JSInterop.SetupVoid("configureDataDownload", (_) => true);
        JSInterop.SetupVoid("configureDeepLinks", (_) => true);
        JSInterop.SetupVoid("renderChart", (_) => true);

        // Act
        var actual = RenderComponent<Home>();

        // Assert
        actual.WaitForAssertion(
            () =>
            {
                actual.Find("[name='repo']").ShouldNotBeNull();
                actual.Find("[name='branch']").ShouldNotBeNull();
                actual.Find("[id='branch']").ShouldNotBeNull();
                actual.FindAll(".benchmark-set").Count.ShouldBe(4);
                actual.FindAll(".benchmark-chart").Count.ShouldBe(9);
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
            "Duplicate[1]",
            "Duplicate[2]",
            "Duplicate[Job]",
            "Duplicate[Job][1]",
            "Duplicate[Job][2]",
        ]);

        grouped["Unique"].Single().Result.Value.ShouldBe(1);
        grouped["Duplicate"].Single().Result.Value.ShouldBe(10);
        grouped["Duplicate[1]"].Single().Result.Value.ShouldBe(20);
        grouped["Duplicate[2]"].Single().Result.Value.ShouldBe(30);
        grouped["Duplicate[Job]"].Single().Result.Value.ShouldBe(40);
        grouped["Duplicate[Job][1]"].Single().Result.Value.ShouldBe(50);
        grouped["Duplicate[Job][2]"].Single().Result.Value.ShouldBe(60);
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
}
