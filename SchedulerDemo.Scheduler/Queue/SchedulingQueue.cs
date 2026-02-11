using System.Threading.Channels;

namespace SchedulerDemo.Scheduler.Queue
{
    public class SchedulingQueue : ISchedulingQueue
    {
        private readonly Channel<ScheduleJob> _channel;

        public SchedulingQueue(int capacity)
        {
            _channel = Channel.CreateBounded<ScheduleJob>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public async IAsyncEnumerable<ScheduleJob> DequeueAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            while (await _channel.Reader.WaitToReadAsync(ct))
                while (_channel.Reader.TryRead(out var job))
                    yield return job;
        }

        public Task EnqueueAsync(ScheduleJob job, CancellationToken ct)
        {
            return _channel.Writer.WriteAsync(job, ct).AsTask();
        }
    }
}
