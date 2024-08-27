// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.Benchmarks;

[CollectionDefinition(Name)]
public sealed class DashboardCollection : ICollectionFixture<DashboardFixture>
{
    public const string Name = "Dashboard collection";
}
