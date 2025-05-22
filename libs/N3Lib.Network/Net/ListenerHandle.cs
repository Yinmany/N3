namespace N3Lib.Network;

internal class ListenerHandle : IDisposable
{
    private readonly IDisposable _listener;
    private readonly CancellationTokenRegistration _disposeToken;
    private bool _isDispose;

    public ListenerHandle(IDisposable listener, CancellationToken cancellationToken)
    {
        _listener = listener;
        _disposeToken = cancellationToken.Register(this.Dispose);
    }

    public void Dispose()
    {
        if (_isDispose)
            return;
        _isDispose = true;
        _disposeToken.Dispose();
        _listener.Dispose();
    }
}