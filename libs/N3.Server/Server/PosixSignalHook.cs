using System.Runtime.InteropServices;

namespace N3;

public class PosixSignalHook : Singleton<PosixSignalHook>, IDisposable
{
    private readonly PosixSignalRegistration _sigInt;
    private readonly PosixSignalRegistration _sigQuit;
    private readonly PosixSignalRegistration _sigTerm;
    private readonly TaskCompletionSource _exitTcs;
    private readonly List<Func<Task>> _stopList = new();

    private PosixSignalHook()
    {
        _exitTcs = new TaskCompletionSource();
        Action<PosixSignalContext> handler = this.HandlePosixSignal;
        _sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
        _sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
        _sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
    }

    private void HandlePosixSignal(PosixSignalContext context)
    {
        context.Cancel = true;
        _ = RunAsync();
        return;

        async Task RunAsync()
        {
            try
            {
                List<Task> tasks = new List<Task>();
                foreach (var cb in _stopList)
                {
                    tasks.Add(cb());
                }

                await Task.WhenAll(tasks);
                _exitTcs.SetResult();
            }
            catch (Exception e)
            {
                _exitTcs.SetException(e);
            }
        }
    }


    public void AddStopCallback(Func<Task> cb)
    {
        _stopList.Add(cb);
    }

    public void RemoveStopCallback(Func<Task> cb)
    {
        _stopList.Remove(cb);
    }

    public Task WaitForExitAsync()
    {
        return _exitTcs.Task;
    }

    public void Dispose()
    {
        _sigInt.Dispose();
        _sigQuit.Dispose();
        _sigTerm.Dispose();
    }
}