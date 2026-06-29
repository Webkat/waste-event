using WasteEvent;

namespace WasteEventApi.Contracts;

/// <summary>
/// Translates the library's polymorphic <see cref="WasteEvent.WasteEvent"/> (whose
/// Context is typed as <c>object?</c>) into a flat, discriminated <see cref="WasteEventDto"/>.
/// One switch, three cases — the single place that knows about the internal context types.
/// </summary>
public static class WasteEventMapper
{
    public static WasteEventDto ToDto(WasteEvent.WasteEvent evt) => evt.EventType switch
    {
        WasteEventType.RoutePositionUpdate => MapPosition(evt),
        WasteEventType.RequestCreated => MapRequest(evt),
        WasteEventType.ServiceUpdate => MapService(evt),
        _ => throw new ArgumentOutOfRangeException(
            nameof(evt), evt.EventType, "Unknown waste event type."),
    };

    private static WasteEventDto MapPosition(WasteEvent.WasteEvent evt)
    {
        var ctx = (RouteUpdateEventContext)evt.Context!;
        return new WasteEventDto
        {
            EventType = nameof(WasteEventType.RoutePositionUpdate),
            HappenedAt = evt.HappenedAt,
            Payload = new RoutePositionPayload
            {
                CarId = ctx.CarId,
                DeviceId = ctx.DeviceId,
                RouteId = ctx.RouteId,
                Lon = ctx.Position?.Longitude,
                Lat = ctx.Position?.Latitude,
                Speed = ctx.Speed,
                // The feed uses -1 as a sentinel for "no heading" — surface it as null.
                Heading = ctx.Heading < 0 ? null : ctx.Heading,
            },
        };
    }

    private static WasteEventDto MapRequest(WasteEvent.WasteEvent evt)
    {
        var ctx = (RequestCreatedEventContext)evt.Context!;
        return new WasteEventDto
        {
            EventType = nameof(WasteEventType.RequestCreated),
            HappenedAt = evt.HappenedAt,
            Payload = new RequestCreatedPayload
            {
                RequestId = ctx.RequestId,
                LocationId = ctx.LocationId,
                ServiceId = ctx.ServiceId,
                Lon = ctx.Position?.Longitude,
                Lat = ctx.Position?.Latitude,
            },
        };
    }

    private static WasteEventDto MapService(WasteEvent.WasteEvent evt)
    {
        var ctx = (ServiceUpdateEventContext)evt.Context!;
        return new WasteEventDto
        {
            EventType = nameof(WasteEventType.ServiceUpdate),
            HappenedAt = evt.HappenedAt,
            Payload = new ServiceUpdatePayload
            {
                ServiceId = ctx.ServiceId,
                LocationId = ctx.LocationId,
                Status = ctx.Status,
                RouteId = ctx.RouteId,
                HandledByCarId = ctx.HandledByCarId,
                HandledByDeviceId = ctx.HandledByDeviceId,
            },
        };
    }
}
