using System.Text.Json;
using SteamPlatform.Api.Infrastructure;

namespace SteamPlatform.Api.Tests;

public sealed class UtcDateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeJsonConverter() }
    };

    [Fact]
    public void Serialize_UnspecifiedOracleTimestamp_EmitsUtcDesignator()
    {
        var value = new DateTime(2026, 8, 25, 16, 6, 40, DateTimeKind.Unspecified);

        var json = JsonSerializer.Serialize(value, Options);

        Assert.Equal("\"2026-08-25T16:06:40.0000000Z\"", json);
    }

    [Fact]
    public void Deserialize_TimestampWithoutOffset_TreatsItAsUtc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2026-08-25T16:06:40\"", Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 8, 25, 16, 6, 40, DateTimeKind.Utc), value);
    }
}
