// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MartinCostello.Benchmarks.Models;

internal sealed class UnixEpochDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long timestamp = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        long timestamp = value.ToUnixTimeMilliseconds();
        writer.WriteNumberValue(timestamp);
    }
}
