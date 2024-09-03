// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Bunit;
using MartinCostello.Benchmarks.Models;

namespace MartinCostello.Benchmarks.Components;

public class BenchmarkSuiteTests : DashboardTestContext
{
    [Fact]
    public void BenchmarkSuite_Renders_Correctly()
    {
        // Arrange
        string name = "Suite";
        var benchmarks = new Dictionary<string, IList<BenchmarkItem>>()
        {
            ["Benchmark1"] = [new(new(), new() { Value = 1 })],
            ["Benchmark2"] = [new(new(), new() { Value = 2 })],
        };

        JSInterop.SetupVoid("renderChart", (args) => args.Arguments.Count is 2 && args.Arguments[0] is "Suite-Benchmark1-chart");
        JSInterop.SetupVoid("renderChart", (args) => args.Arguments.Count is 2 && args.Arguments[0] is "Suite-Benchmark2-chart");

        // Act
        var actual = RenderComponent<BenchmarkSuite>((builder) =>
        {
            builder.Add((p) => p.Name, name)
                   .Add((p) => p.Benchmarks, benchmarks);
        });

        // Assert
        var element = actual.Find($"[id='{name}']");
        element.ShouldNotBeNull();

        actual.Find("h2").TextContent.Trim().ShouldBe(name);

        var charts = actual.FindAll(".benchmark-chart");
        charts.Count.ShouldBe(2);

        charts[0].GetAttribute("name").ShouldBe("Benchmark1");
        charts[1].GetAttribute("name").ShouldBe("Benchmark2");
    }
}
