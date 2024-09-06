// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using Bunit;
using MartinCostello.Benchmarks.Models;

namespace MartinCostello.Benchmarks.Components;

public class BenchmarkSuiteTests : DashboardTestContext
{
    [Fact]
    public void BenchmarkSuite_Renders_Correctly()
    {
        // Arrange
        string suite = "Suite";

        var benchmarks = new Dictionary<string, IList<BenchmarkItem>>()
        {
            ["Benchmark1"] = [new(new(), new() { Value = 1 })],
            ["Benchmark2"] = [new(new(), new() { Value = 2 })],
        };

        foreach (string benchmark in benchmarks.Keys)
        {
            JSInterop.SetupVoid("renderChart", (args) => RenderArgumentsAreValid($"{suite}-{benchmark}-chart", args.Arguments));
        }

        // Act
        var actual = RenderComponent<BenchmarkSuite>((builder) =>
        {
            builder.Add((p) => p.Name, suite)
                   .Add((p) => p.Benchmarks, benchmarks);
        });

        // Assert
        var element = actual.Find($"[id='{suite}']");
        element.ShouldNotBeNull();

        actual.Find("h2").GetInnerText().ShouldBe($"{suite} #");

        var charts = actual.FindAll(".benchmark-chart");
        charts.Count.ShouldBe(2);

        charts[0].GetAttribute("name").ShouldBe("Benchmark1");
        charts[1].GetAttribute("name").ShouldBe("Benchmark2");

        actual.FindAll(".download-chart").Count.ShouldBe(2);
    }

    private static bool RenderArgumentsAreValid(string expectedChartId, IReadOnlyList<object?> arguments)
    {
        if (arguments.Count is not 2 ||
            arguments[0] is not string chartId ||
            !string.Equals(expectedChartId, chartId, StringComparison.Ordinal) ||
            arguments[1] is not string configString)
        {
            return false;
        }

        var node = JsonObject.Parse(configString);
        var config = node.ShouldBeOfType<JsonObject>();

        config.ShouldNotBeNull();
        config["colors"].ShouldNotBeNull().GetValueKind().ShouldBe(JsonValueKind.Object);
        config["dataset"].ShouldNotBeNull().GetValueKind().ShouldBe(JsonValueKind.Array);
        config["errorBars"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        config["imageFormat"].ShouldNotBeNull().GetValue<string>().ShouldBe("png");
        config["name"].ShouldNotBeNull().GetValue<string>().ShouldNotBeNullOrWhiteSpace();
        config["suiteName"].ShouldNotBeNull().GetValue<string>().ShouldNotBeNullOrWhiteSpace();

        return true;
    }
}
