namespace N3Lib.Network;

public class SocketSchedulers
{
    private readonly int _numSchedulers;
    private readonly IOQueue[] _schedulers;
    private int _nextScheduler;

    public SocketSchedulers(bool useThreadPool = true, int? ioQueueCount = null)
    {
        _numSchedulers = ioQueueCount ?? Math.Min(Environment.ProcessorCount, 16);
        _schedulers = new IOQueue[_numSchedulers];

        for (var i = 0; i < _numSchedulers; i++)
        {
            _schedulers[i] = new IOQueue(useThreadPool);
        }
    }

    public IOQueue GetScheduler() => _schedulers[++_nextScheduler % _numSchedulers];
}