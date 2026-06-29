using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace WasteEvent;

internal sealed class PositionRow
{
    public DateTime Time { get; init; }
    public string Coordinates { get; init; } = "";
    public int DeviceId { get; init; }
    public int CarId { get; init; }
    public int RouteId { get; init; }
    public float Speed { get; init; }
    public float Heading { get; init; }
}

internal sealed class RequestRow
{
    public int RequestId { get; init; }
    public int LocationId { get; init; }
    public string Coordinates { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ServiceId { get; init; }
}

internal sealed class ServiceRow
{
    public int Id { get; init; }
    public int LocationId { get; init; }
    public int StatusId { get; init; }
    public int? RouteId { get; init; }
    public int? HandledByDeviceId { get; init; }
    public int? HandledByCarId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

internal sealed class PositionMap : ClassMap<PositionRow>
{
    public PositionMap()
    {
        Map(m => m.Time).Name("time").TypeConverter<UtcDateTimeConverter>();
        Map(m => m.Coordinates).Name("coordinates");
        Map(m => m.DeviceId).Name("device_id");
        Map(m => m.CarId).Name("car_id");
        Map(m => m.RouteId).Name("route_id");
        Map(m => m.Speed).Name("speed");
        Map(m => m.Heading).Name("heading");
    }
}

internal sealed class RequestMap : ClassMap<RequestRow>
{
    public RequestMap()
    {
        Map(m => m.RequestId).Name("request_id");
        Map(m => m.LocationId).Name("location_id");
        Map(m => m.Coordinates).Name("coordinates");
        Map(m => m.CreatedAt).Name("created_at").TypeConverter<UtcDateTimeConverter>();
        Map(m => m.UpdatedAt).Name("updated_at").TypeConverter<UtcDateTimeConverter>();
        Map(m => m.ServiceId).Name("service_id");
    }
}

internal sealed class ServiceMap : ClassMap<ServiceRow>
{
    public ServiceMap()
    {
        Map(m => m.Id).Name("id");
        Map(m => m.LocationId).Name("location_id");
        Map(m => m.StatusId).Name("status_id");
        Map(m => m.RouteId).Name("route_id");
        Map(m => m.HandledByDeviceId).Name("handled_by_device_id");
        Map(m => m.HandledByCarId).Name("handled_by_car_id");
        Map(m => m.CreatedAt).Name("created_at").TypeConverter<UtcDateTimeConverter>();
        Map(m => m.UpdatedAt).Name("updated_at").TypeConverter<UtcDateTimeConverter>();
    }
}

internal static class WktParser
{
    public static Coordinate? ParsePoint(ReadOnlySpan<char> wkt)
    {
        if (wkt.Length < 10)
        {
            return null;
        }

        var inner = wkt["POINT (".Length..^1]; // "lon lat"
        var spaceIdx = inner.IndexOf(' ');
        return new Coordinate
        {
            Longitude = double.Parse(inner[..spaceIdx], CultureInfo.InvariantCulture),
            Latitude = double.Parse(inner[(spaceIdx + 1)..], CultureInfo.InvariantCulture)
        };
    }
}

internal sealed class UtcDateTimeConverter : DateTimeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        var dt = (DateTime?)base.ConvertFromString(text, row, memberMapData);
        return dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : dt;
    }
}

internal sealed class CsvCursor<TRow> : IDisposable
{
    private readonly CsvReader _csv;
    private TRow? _peeked;
    private bool _done;

    public CsvCursor(CsvReader csv) => _csv = csv;

    /// Null when the stream is exhausted.
    public TRow? Current => _peeked;

    public async ValueTask AdvanceAsync()
    {
        if (_done) return;
        if (await _csv.ReadAsync())
            _peeked = _csv.GetRecord<TRow>();
        else
        {
            _peeked = default;
            _done = true;
        }
    }
    
    public void Dispose()
    {
        _csv.Dispose();
    }
}

