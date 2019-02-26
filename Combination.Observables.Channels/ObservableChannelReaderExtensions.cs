using System;
using System.Threading;
using System.Threading.Channels;

namespace Combination.Observables.Channels
{
    public static class ObservableChannelReaderExtensions
    {
        private const int defaultCapacity = 8;

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, UnboundedChannelOptions unboundedChannelOptions, bool failOnDrop = true)
        {
            return AsChannelReader(topic, unboundedChannelOptions, CancellationToken.None, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, UnboundedChannelOptions unboundedChannelOptions, CancellationToken cancellationToken, bool failOnDrop = true)
        {
            var channel = Channel.CreateUnbounded<T>(unboundedChannelOptions);
            return AsChannelReaderInternal(topic, channel, cancellationToken, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, int capacity, bool failOnDrop = true)
        {
            return AsChannelReader(topic, capacity, CancellationToken.None, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, BoundedChannelOptions boundedChannelOptions, bool failOnDrop = true)
        {
            return AsChannelReader(topic, boundedChannelOptions, CancellationToken.None, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, CancellationToken cancellationToken, bool failOnDrop = true)
        {
            return AsChannelReader(topic, defaultCapacity, cancellationToken, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, bool failOnDrop = true)
        {
            return AsChannelReader(topic, defaultCapacity, CancellationToken.None, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, int capacity, CancellationToken cancellationToken, bool failOnDrop = true)
        {
            var channel = Channel.CreateBounded<T>(capacity);
            return AsChannelReaderInternal(topic, channel, cancellationToken, failOnDrop);
        }

        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> topic, BoundedChannelOptions boundedChannelOptions, CancellationToken cancellationToken, bool failOnDrop = true)
        {
            var channel = Channel.CreateBounded<T>(boundedChannelOptions);
            return AsChannelReaderInternal(topic, channel, cancellationToken, failOnDrop);
        }

        private static ChannelReader<T> AsChannelReaderInternal<T>(IObservable<T> topic, Channel<T> channel, CancellationToken cancellationToken, bool failOnDrop)
        {
            var disposable = topic.Subscribe(value =>
                {
                    if (!channel.Writer.TryWrite(value) && failOnDrop)
                    {
                        try
                        {
                            channel.Writer.Complete(new BufferOverflowException("ChannelReader buffer overflow"));
                        }
                        catch
                        {
                            // Error in error handling
                        }
                    }
                },
                error => channel.Writer.TryComplete(error),
                () => channel.Writer.TryComplete());

            var connectionAbortedRegistration = cancellationToken.Register(() =>
            {
                channel.Writer.TryComplete();

                // Read lingering items in the Reader to make sure it completes
                while (channel.Reader.TryRead(out var _))
                {
                    // Do nothing
                }
            });

            channel.Reader.Completion.ContinueWith(task =>
            {
                disposable.Dispose();
                connectionAbortedRegistration.Dispose();
            });

            return channel.Reader;
        }
    }
}
