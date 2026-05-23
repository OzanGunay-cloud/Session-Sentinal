using System.Threading.Channels;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence.AnomalyLogging;

public sealed class AnomalyLogChannel
{
    private readonly Channel<AnomalyLog> _channel;

    public AnomalyLogChannel(int capacity)
    {
        // Bounded capacity prevents unbounded memory growth under burst traffic.
        _channel = Channel.CreateBounded<AnomalyLog>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public ValueTask QueueAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(anomalyLog, cancellationToken);

    public IAsyncEnumerable<AnomalyLog> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
