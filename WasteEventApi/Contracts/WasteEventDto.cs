using WasteEvent;

namespace WasteEventApi.Contracts;

/// <summary>
/// Flat, frontend-facing envelope for every event. The <see cref="EventType"/>
/// discriminator tells the client which shape <see cref="Payload"/> has, so the
/// Vue side never has to inspect a polymorphic .NET object.
/// </summary>
public sealed record WasteEventDto
{
    public required string EventType { get; init; }

    public DateTime HappenedAt { get; init; }

    public required object Payload { get; init; }
}

public sealed record RoutePositionPayload
{
    public required int CarId { get; init; }
    public required int DeviceId { get; init; }
    public required int RouteId { get; init; }
    public double? Lon { get; init; }
    public double? Lat { get; init; }
    public float Speed { get; init; }

    /// <summary>Compass heading in degrees (0–360). Null when the truck reported -1 (unknown).</summary>
    public float? Heading { get; init; }
}

public sealed record RequestCreatedPayload
{
    public required int RequestId { get; init; }
    public required int LocationId { get; init; }
    public required int ServiceId { get; init; }
    public double? Lon { get; init; }
    public double? Lat { get; init; }
}

public sealed record ServiceUpdatePayload
{
    public required int ServiceId { get; init; }
    public required int LocationId { get; init; }

    /// <summary>Serialized as a string ("Complete", "Await", …) via JsonStringEnumConverter.</summary>
    public required ServiceStatus Status { get; init; }

    public int? RouteId { get; init; }
    public int? HandledByCarId { get; init; }
    public int? HandledByDeviceId { get; init; }
}
