# Benchmarks Dashboard

[![Deployment status][build-badge]][build-status]

## Introduction

A dashboard for visualising [benchmark results][benchmarks-data] from my repositories using
[BenchmarkDotNet][benchmarkdotnet] and [martincostello/benchmarkdotnet-results-publisher][benchmarkdotnet-results-publisher].

The dashboard is a [Blazor WASM][blazor] static application deployed to [a GitHub Pages site][site].

## Building and Testing

Compiling the application yourself requires Git and the [.NET SDK][dotnet-sdk] to be installed.

To build and test the application locally from a terminal/command-line, run the
following set of commands:

```powershell
git clone https://github.com/martincostello/benchmarks-dashboard.git
cd benchmarks-dashboard
./build.ps1
```

## Feedback

Any feedback or issues can be added to the issues for this project in [GitHub][issues].

## Repository

The repository is hosted in [GitHub][repo]: <https://github.com/martincostello/benchmarks-dashboard.git>

## License

This project is licensed under the [Apache 2.0][license] license.

[benchmarkdotnet]: https://github.com/dotnet/BenchmarkDotNet "The BenchmarkDotNet repository on GitHub.com"
[benchmarkdotnet-results-publisher]: https://github.com/martincostello/benchmarkdotnet-results-publisher "A GitHub Action that publishes results from BenchmarkDotNet benchmarks to a GitHub repository"
[benchmarks-data]: https://github.com/martincostello/benchmarks "The GitHub repository containing the benchmark results"
[blazor]: https://learn.microsoft.com/aspnet/core/blazor "ASP.NET Core Blazor"
[build-badge]: https://github.com/martincostello/benchmarks-dashboard/actions/workflows/build.yml/badge.svg?branch=main&event=push
[build-status]: https://github.com/martincostello/benchmarks-dashboard/actions?query=workflow%3Abuild+branch%3Amain+event%3Apush "Continuous Integration for this project"
[dotnet-sdk]: https://dotnet.microsoft.com/download "Download the .NET SDK"
[issues]: https://github.com/martincostello/benchmarks-dashboard/issues "Issues for this project on GitHub.com"
[license]: https://www.apache.org/licenses/LICENSE-2.0.txt "The Apache 2.0 license"
[repo]: https://github.com/martincostello/benchmarks-dashboard "This project on GitHub.com"
[site]: https://benchmarks.martincostello.com
