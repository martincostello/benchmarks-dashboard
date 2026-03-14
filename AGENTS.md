# Coding Agent Instructions

This file provides guidance to coding agents when working with code in this repository.

## Build, test, and lint commands

- Preferred full validation: `.\build.ps1`
  - `build.ps1` bootstraps the SDK from `global.json` if needed, publishes `src\Dashboard\Dashboard.csproj`, and then runs `dotnet test --configuration Release`.
- Publish-only: `.\build.ps1 -SkipTests`
- App publish directly: `dotnet publish src\Dashboard\Dashboard.csproj`
- Full test suite: `dotnet test tests\Dashboard.Tests\Dashboard.Tests.csproj --configuration Release`
- Single test: `dotnet test tests\Dashboard.Tests\Dashboard.Tests.csproj --configuration Release --filter "FullyQualifiedName~MartinCostello.Benchmarks.GitHubServiceTests.Can_Sign_In_And_Out"`
- UI tests only: `dotnet test tests\Dashboard.Tests\Dashboard.Tests.csproj --configuration Release --filter "Category=UI"`
  - UI tests install Playwright browsers in `UITest.InitializeAsync()` and start the Blazor app through `DashboardFixture`, so they are slower and more integration-heavy than the bUnit/unit tests.
- Markdown linting used in CI: `markdownlint-cli2 "**/*.md" --config .markdownlint.json`
- PowerShell linting used in CI:

  ```powershell
  $settings = @{
    IncludeDefaultRules = $true
    Severity = @("Error", "Warning")
  }
  $issues = Invoke-ScriptAnalyzer -Path . -Recurse -ReportSummary -Settings $settings
  if ($issues.Count -gt 0) { exit 1 }
  ```

- Workflow linting is defined in `.github\workflows\lint.yml` with `actionlint` and `zizmor`.

## High-level architecture

- This repository is a Blazor WebAssembly static app in `src\Dashboard`. CI publishes the site and uploads `artifacts\publish\Dashboard\release\wwwroot` for GitHub Pages deployment.
- `Program.cs` is the composition root. It binds the `Dashboard` configuration section into `DashboardOptions`, registers `Blazored.LocalStorage`, and wires the app services: `GitHubDeviceTokenService`, `GitHubClient`, `GitHubService`, and `GitHubTokenStore`.
- `GitHubService` is the stateful orchestration layer. It owns the selected repository, branch, current commit, benchmark payload, and current user, and raises `OnUserChanged` for UI updates.
- `GitHubClient` is the HTTP boundary. It handles GitHub REST calls, device-flow token exchange, and benchmark-data downloads. For public GitHub.com repositories it reads benchmark JSON from `raw.githubusercontent.com`; for private repositories or GitHub Enterprise it switches to the GitHub API and adds auth/version headers.
- Routing is intentionally small and centralized in `Routes.cs`: the home dashboard page and the token/device-flow page. `Pages\Home.razor.cs` also binds `repo` and `branch` from the query string so deep links are part of the main app flow.
- Benchmark rendering is split between C# and JavaScript:
  - `Pages\Home.razor.cs` loads benchmark data, groups duplicate jobs, normalizes time and memory units, and serializes the full payload for download/deep-link helpers.
  - `Components\Benchmark.razor.cs` serializes the chart-specific payload and calls `renderChart`.
  - `wwwroot\app.js` owns Plotly rendering, theme refresh, deep-link setup, download/clipboard helpers, and chart interactions.
- Layout behavior is also JS-backed. `MainLayout.razor.cs` calls `configureToolTips`, and `Navbar.razor.cs` listens to `GitHubService.OnUserChanged`, builds the data-repository URL from options, and delegates theme switching/sign-out behavior.
- The test project in `tests\Dashboard.Tests` mixes three levels of testing in one place:
  - bUnit/component tests and unit tests use `DashboardTestContext`.
  - HTTP interactions are mocked with `JustEat.HttpClientInterception`, and missing registrations are treated as failures.
  - Browser/UI coverage uses Playwright plus page models in `PageModels\`, with visual snapshots stored under `tests\Dashboard.Tests\snapshots`.

## Key conventions

- Use code-behind partial classes for pages, layout components, and reusable components. The repository consistently pairs `.razor` files with `.razor.cs` files instead of putting significant logic inline in markup.
- Keep route strings centralized in `Routes.cs` and keep query-string-driven state in the page code-behind. `Home` depends on `repo` and `branch` query parameters for deep-linking.
- Read app configuration through `IOptions<DashboardOptions>` rather than directly from configuration. The options object is the single place for GitHub endpoints, repository lists, dataset colors, image format, and auth settings.
- Use `AppJsonSerializerContext` for application JSON payloads exchanged with GitHub and benchmark data. The project relies on source-generated `System.Text.Json` metadata rather than reflection-based serialization.
- Preserve the `GitHubService` / `GitHubClient` separation when changing data flow:
  - `GitHubClient` should stay focused on HTTP and serialization.
  - `GitHubService` should own app state transitions and higher-level loading/sign-in behavior.
- When changing benchmark presentation, keep the normalization and duplicate-job handling in `Home.razor.cs` aligned with the chart payload expected by `Benchmark.razor.cs` and `wwwroot\app.js`.
- The app persists the GitHub token through `GitHubTokenStore` and `Blazored.LocalStorage`; authentication changes usually touch the token store, device token service, `GitHubService`, and the `Token` page together.
- Tests assume explicit registration of every outbound HTTP call. In `DashboardTestContext`, `HttpClientInterceptorOptions().ThrowsOnMissingRegistration()` is intentional; add new response registrations instead of loosening interception behavior.
- UI tests are categorized with `[Category("UI")]`, automatically install Playwright, and launch the real app via `DashboardFixture`. Changes to app startup, routes, or static assets can affect those tests even when unit tests stay green.
- Build outputs go under `artifacts\` because `Directory.Build.props` sets `UseArtifactsOutput=true`; the CI publish/deploy steps depend on that layout.

## General guidelines

- Always ensure code compiles with no warnings or errors and tests pass locally before pushing changes.
- Do not use APIs marked with `[Obsolete]`.
- Bug fixes should **always** include a test that would fail without the corresponding fix.
- Do not introduce new dependencies unless specifically requested.
- Do not update existing dependencies unless specifically requested.
