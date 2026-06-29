using System.Collections.Concurrent;
using System.Threading.Channels;
using WasteEventApi.Contracts;

namespace WasteEventApi.Streaming;

/// <summary>
/// Fans a single event stream out to every connected client. Registered as a
/// singleton: the <see cref="WasteEventPublisher"/> reads the source once and
/// calls <see cref="Publish"/>; each SSE connection gets its own channel via
/// <see cref="Subscribe"/>. Channels are bounded with DropOldest, so one slow
/// client can never block the publisher or the other clients.
/// </summary>
public sealed class WasteEventBroadcaster
{
    private const int BufferCapacity = 256;

    private readonly ConcurrentDictionary<Guid, Channel<WasteEventDto>> _subscribers = new();

    public Subscription Subscribe()
    {
        var channel = Channel.CreateBounded<WasteEventDto>(new BoundedChannelOptions(BufferCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        var id = Guid.NewGuid();
        _subscribers[id] = channel;
        return new Subscription(this, id, channel.Reader);
    }

    public void Publish(WasteEventDto dto)
    {
        foreach (var channel in _subscribers.Values)
        {
            // TryWrite never blocks; with DropOldest it always succeeds.
            channel.Writer.TryWrite(dto);
        }
    }

    private void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    public sealed class Subscription : IDisposable
    {
        private readonly WasteEventBroadcaster _owner;
        private readonly Guid _id;

        internal Subscription(WasteEventBroadcaster owner, Guid id, ChannelReader<WasteEventDto> reader)
        {
            _owner = owner;
            _id = id;
            Reader = reader;
        }

        public ChannelReader<WasteEventDto> Reader { get; }

        public void Dispose() => _owner.Unsubscribe(_id);
    }
}
