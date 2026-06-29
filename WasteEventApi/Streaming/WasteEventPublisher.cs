using WasteEvent;
using WasteEventApi.Contracts;

namespace WasteEventApi.Streaming;

/// <summary>
/// Hosted service that drives the simulation. It consumes
/// <see cref="WasteEventService.Listen"/> exactly once for the lifetime of the
/// app and hands each mapped event to the <see cref="WasteEventBroadcaster"/>.
/// The library does the real-time pacing; we just relay.
/// </summary>
public sealed class WasteEventPublisher : BackgroundService
{
    private readonly WasteEventBroadcaster _broadcaster;
    private readonly ILogger<WasteEventPublisher> _logger;
    private readonly double _speedMultiplier;

    public WasteEventPublisher(
        WasteEventBroadcaster broadcaster,
        IConfiguration configuration,
        ILogger<WasteEventPublisher> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
        _speedMultiplier = configuration.GetValue("WasteEvents:SpeedMultiplier", 120d);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var service = new WasteEventService();
        _logger.LogInformation("Replaying waste events at {Speed}x simulated speed.", _speedMultiplier);

        try
        {
            await foreach (var evt in service.Listen(_speedMultiplier, stoppingToken))
            {
                _broadcaster.Publish(WasteEventMapper.ToDto(evt));
            }

            _logger.LogInformation("Waste event stream completed.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}
