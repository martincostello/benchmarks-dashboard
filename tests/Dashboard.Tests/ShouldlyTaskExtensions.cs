﻿// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

#pragma warning disable IDE0130
namespace Shouldly;

public static class ShouldlyTaskExtensions
{
    public static async Task ShouldBe(this Task<string> task, string expected)
    {
        string actual = await task;
        actual.ShouldBe(expected);
    }

    public static async Task ShouldBe<T>(this Task<T> task, T expected)
    {
        T actual = await task;
        actual.ShouldBe(expected);
    }

    public static async Task ShouldBeFalse(this Task<bool> task)
    {
        bool actual = await task;
        actual.ShouldBeFalse();
    }

    public static async Task ShouldBeTrue(this Task<bool> task)
    {
        bool actual = await task;
        actual.ShouldBeTrue();
    }

    public static async Task ShouldNotBeNullOrWhiteSpace(this Task<string> task)
    {
        string actual = await task;
        actual.ShouldNotBeNullOrWhiteSpace();
    }
}
