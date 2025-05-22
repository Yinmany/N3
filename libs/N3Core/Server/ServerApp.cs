using Cysharp.Threading.Tasks;

namespace N3Core;

public interface IServerInit
{
    UniTask OnInit(ServerApp app);
    UniTask OnUnInit(ServerApp app);
}

/// <summary>
/// 单线程逻辑
/// </summary>
public class ServerApp : Entity
{
    public ushort ServerId { get; }
    public ushort ServeType { get; }

    public string Name { get; }

    public WorkQueue WorkQueue => _workQueue;

    private readonly CancellationTokenSource _exitCts = new();
    private readonly ThreadWorkQueue _workQueue;
    private readonly Thread _thread;
    private bool _isDisposed = false;

    public ServerApp(ushort serverId, ushort serverType, string name) : base(Did.Make(serverId))
    {
        ServerId = serverId;
        ServeType = serverType;
        this.Name = name;
        _thread = new Thread(Run);
        _workQueue = new ThreadWorkQueue(_thread.ManagedThreadId);
        _thread.IsBackground = true;
        _thread.Start();
    }

    public Task Shutdown()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        _workQueue.Post(async void (_) =>
        {
            try
            {
                if (_isDisposed)
                    return;

                UniTask? task = TypeManager.Ins.Get(ServeType)?.Init?.OnUnInit(this);
                if (task != null)
                    await task.Value;
                tcs.SetResult();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            finally
            {
                Dispose();
            }
        }, null);
        return tcs.Task;
    }

    private void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        Destroy();
        _exitCts.Cancel();
        _exitCts.Dispose();
        _thread.Join();
    }

    private void Init()
    {
        // 初始化invoke
        _workQueue.PostNext(_ => { TypeManager.Ins.Get(ServeType)?.Init?.OnInit(this).Forget(); }, null);
    }

    private void Run()
    {
        try
        {
            SynchronizationContext.SetSynchronizationContext(_workQueue);
            CancellationToken stopToken = _exitCts.Token;

            var eventSystem = this.AddComp(new EventSystem(ServeType));
            Init();

            STime time = STime.Start();
            while (!stopToken.IsCancellationRequested)
            {
                long deltaTimeMs = time.Record();
                time.Restart();
                _workQueue.Update();
                eventSystem?.Update();

                // 耗时100ms就不休眠，避免频繁休眠
                long ms = time.Record();
                if (ms < 100)
                {
                    Thread.Sleep(1);
                }
            }
        }
        catch (Exception e)
        {
            SLog.Error(e, "错误");
        }
    }

    public override string ToString()
    {
        return $"{Name}, {ServerId}, {ServeType}";
    }
}