// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using MartinCostello.Benchmarks.Models;

namespace MartinCostello.Benchmarks.Pages;

public static class HomeTests
{
    public static TheoryData<double[], double?[], double[], string?, double?[], string?> NormalizationTestCases()
    {
        var testCases = new TheoryData<double[], double?[], double[], string?, double?[], string?>()
        {
            { [], [], [], null, [], null },
            { [1, 2, 3], [null, null, null], [1, 2, 3], null, [null, null, null], null },
            { [1, 2, 3], [1, 2, 3], [1, 2, 3], null, [1, 2, 3], null },
            { [1, 2, 3], [1, null, 3], [1, 2, 3], null, [1, null, 3], null },
            { [100, 1000, 1000], [null, null, null], [100, 1000, 1000], null, [null, null, null], null },
            { [100, 1000, 1000], [200, 300, 1000], [100, 1000, 1000], null, [200, 300, 1000], null },
            { [1234, 2345, 3456], [null, null, null], [1.234, 2.345, 3.456], "µs", [null, null, null], null },
            { [1234, 2345, 3456], [6789, 7900, 8901], [1.234, 2.345, 3.456], "µs", [6.789, 7.900, 8.901], "KB" },
            { [1234, 2345, 3456], [6789000, 7900000, 8901000], [1.234, 2.345, 3.456], "µs", [6.789, 7.900, 8.901], "MB" },
            { [1234, 2345, 34560], [null, null, null], [1.234, 2.345, 34.560], "µs", [null, null, null], null },
            { [123400, 234500, 3456000], [null, null, null], [123.4, 234.5, 3456], "µs", [null, null, null], null },
            { [1234000, 2345000, 3456000], [null, null, null], [1.234, 2.345, 3.456], "ms", [null, null, null], null },
            { [1234000000, 2345000000, 3456000000], [null, null, null], [1.234, 2.345, 3.456], "s", [null, null, null], null },
        };

        return testCases;
    }

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
                Commit = new() { Sha = "abc" },
                Benchmarks =
                [
                    new() { Name = "A", Value = 1 },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 05, 00, TimeSpan.Zero),
                Commit = new() { Sha = "def" },
                Benchmarks =
                [
                    new() { Name = "A", Value = 2 },
                    new() { Name = "B", Value = 2678 },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 01, 00, 05, 15, TimeSpan.Zero),
                Commit = new() { Sha = "def" },
                Benchmarks =
                [
                    new() { Name = "A", Value = 3 },
                    new() { Name = "B", Value = 2497 },
                ],
            },
            new()
            {
                Timestamp = new DateTimeOffset(2024, 09, 02, 00, 05, 00, TimeSpan.Zero),
                Commit = new() { Sha = "123" },
                Benchmarks =
                [
                    new() { Name = "A", Value = 4 },
                    new() { Name = "B", Value = 2642 },
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
        values[0].Result.Unit.ShouldBeNull();
        values[1].Result.Value.ShouldBe(2);
        values[1].Result.Unit.ShouldBeNull();
        values[2].Result.Value.ShouldBe(4);
        values[2].Result.Unit.ShouldBeNull();

        values = actual["B"];

        values.Count.ShouldBe(2);
        values[0].Result.Value.ShouldBe(2.678);
        values[0].Result.Unit.ShouldBe("µs");
        values[1].Result.Value.ShouldBe(2.642);
        values[1].Result.Unit.ShouldBe("µs");
    }
}
