using System.Text.Json;
using System.Text.Json.Serialization;
using WasteEventApi.Streaming;

namespace WasteEventApi.Endpoints;

/// <summary>
/// Registers the Server-Sent Events endpoint that streams waste events to clients.
/// Kept out of Program.cs so startup stays focused on wiring, and the streaming
/// logic lives in one named place.
/// </summary>
public static class EventStreamEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static IEndpointRouteBuilder MapEventStream(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events/stream", async (
            HttpContext context,
            WasteEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            // Disable server-side buffering so each event is flushed immediately.
            context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            using var subscription = broadcaster.Subscribe();

            try
            {
                await foreach (var dto in subscription.Reader.ReadAllAsync(ct))
                {
                    var json = JsonSerializer.Serialize(dto, JsonOptions);
                    await context.Response.WriteAsync($"data: {json}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected — subscription disposes via the using block.
            }
        });

        return app;
    }
}
