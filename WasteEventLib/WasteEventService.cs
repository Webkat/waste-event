using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;

namespace WasteEvent;

public class WasteEventService
{
    private const string PositionsPath = "positions.csv";
    private const string RequestsPath = "requests.csv";
    private const string ServicesPath = "services.csv";

    /// <param name="speedMultiplier">
    ///   How many simulated seconds pass per real second.
    ///   E.g. 60 = 1 simulated minute per real second.
    /// </param>
    /// <param name="cancellationToken"/>
    public async IAsyncEnumerable<WasteEvent> Listen(
        double speedMultiplier = 1.0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        };

        // Open all three CSV readers
        using var posCursor = OpenCursor<PositionRow, PositionMap>(PositionsPath, csvConfig);
        using var reqCursor = OpenCursor<RequestRow, RequestMap>(RequestsPath, csvConfig);
        using var svcCursor = OpenCursor<ServiceRow, ServiceMap>(ServicesPath, csvConfig);

        // Prime each cursor
        await posCursor.AdvanceAsync();
        await reqCursor.AdvanceAsync();
        await svcCursor.AdvanceAsync();

        // Determine the simulation start — earliest event across all sources
        var simStart = Min(
            Timestamp(posCursor.Current),
            Timestamp(reqCursor.Current),
            Timestamp(svcCursor.Current));

        if (simStart is null) yield break; // all empty

        var realStart = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Pick the cursor with the smallest next timestamp
            var (nextTime, source) = EarliestCursor(posCursor, reqCursor, svcCursor);
            if (nextTime is null) break; // all exhausted

            // How long until this event in simulated time from simStart?
            var simOffset = nextTime.Value - simStart.Value;
            // Convert to real-time offset
            var realOffset = TimeSpan.FromTicks((long)(simOffset.Ticks / speedMultiplier));
            var fireAt = realStart + realOffset;

            var delay = fireAt - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);

            WasteEvent? evt = source switch
            {
                0 => MapPosition(posCursor.Current!),
                1 => MapRequest(reqCursor.Current!),
                2 => MapService(svcCursor.Current!),
                _ => null
            };

            // Advance the cursor we just consumed
            switch (source)
            {
                case 0: await posCursor.AdvanceAsync(); break;
                case 1: await reqCursor.AdvanceAsync(); break;
                case 2: await svcCursor.AdvanceAsync(); break;
            }

            if (evt is not null)
                yield return evt;
        }
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    private static CsvCursor<TRow> OpenCursor<TRow, TMap>(string path, CsvConfiguration cfg)
        where TMap : ClassMap<TRow>
    {
        var reader = new StreamReader(path);
        var csv = new CsvReader(reader, cfg);
        csv.Context.RegisterClassMap<TMap>();
        return new CsvCursor<TRow>(csv);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static WasteEvent MapPosition(PositionRow row) => new()
    {
        HappenedAt = row.Time,
        EventType = WasteEventType.RoutePositionUpdate,
        Context = new RouteUpdateEventContext
        {
            Time = row.Time,
            Position = WktParser.ParsePoint(row.Coordinates),
            DeviceId = row.DeviceId,
            CarId = row.CarId,
            RouteId = row.RouteId,
            Speed = row.Speed,
            Heading = row.Heading
        }
    };

    private static WasteEvent MapRequest(RequestRow row) => new()
    {
        HappenedAt = row.CreatedAt,
        EventType = WasteEventType.RequestCreated,
        Context = new RequestCreatedEventContext
        {
            RequestId = row.RequestId,
            LocationId = row.LocationId,
            ServiceId = row.ServiceId,
            Position = WktParser.ParsePoint(row.Coordinates),
            Time = row.CreatedAt
        }
    };

    private static WasteEvent MapService(ServiceRow row) => new()
    {
        HappenedAt = row.UpdatedAt,
        EventType = WasteEventType.ServiceUpdate,
        Context = new ServiceUpdateEventContext
        {
            ServiceId = row.Id,
            LocationId = row.LocationId,
            Status = (ServiceStatus)row.StatusId,
            RouteId = row.RouteId,
            HandledByDeviceId = row.HandledByDeviceId,
            HandledByCarId = row.HandledByCarId,
            Time = row.UpdatedAt
        }
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DateTime? Timestamp(PositionRow? r) => r?.Time;
    private static DateTime? Timestamp(RequestRow? r) => r?.CreatedAt;
    private static DateTime? Timestamp(ServiceRow? r) => r?.UpdatedAt;

    private static DateTime? Min(params DateTime?[] values)
    {
        DateTime? min = null;
        foreach (var v in values)
            if (v.HasValue && (min is null || v.Value < min.Value))
                min = v;
        return min;
    }

    /// Returns (timestamp, sourceIndex) for whichever cursor has the earliest next event.
    /// sourceIndex: 0=positions, 1=requests, 2=services
    private static (DateTime? time, int source) EarliestCursor(
        CsvCursor<PositionRow> pos,
        CsvCursor<RequestRow> req,
        CsvCursor<ServiceRow> svc)
    {
        DateTime? best = null;
        int source = -1;

        void Check(DateTime? t, int idx)
        {
            if (t.HasValue && (best is null || t.Value < best.Value))
            {
                best = t;
                source = idx;
            }
        }

        Check(Timestamp(pos.Current), 0);
        Check(Timestamp(req.Current), 1);
        Check(Timestamp(svc.Current), 2);

        return (best, source);
    }
}





