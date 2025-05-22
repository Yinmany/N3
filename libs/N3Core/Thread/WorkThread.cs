namespace N3Core;

public sealed class WorkThread
{
    public event Action? OnTick;

    public WorkThread()
    {
        var thread = new Thread(Tick)
        {
            IsBackground = true
        };
        thread.Start();
    }

    private void Tick()
    {
        while (true)
        {
            Thread.Sleep(1);
            OnTick?.Invoke();
        }
    }
}