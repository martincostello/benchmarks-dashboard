// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Blazored.LocalStorage;
using MartinCostello.Benchmarks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<DashboardOptions>(builder.Configuration.GetSection("Dashboard"));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped(
    (provider) =>
        new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<GitHubDeviceTokenService>();
builder.Services.AddScoped<GitHubClient>();
builder.Services.AddScoped<GitHubService>();
builder.Services.AddScoped<GitHubTokenStore>();

await builder.Build().RunAsync();

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}
