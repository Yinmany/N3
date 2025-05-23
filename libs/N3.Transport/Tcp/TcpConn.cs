using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using N3.Buffer;

namespace N3.Network;

public class TcpConn : IDisposable
{
    private readonly Socket _socket;
    private readonly SocketReceiver _socketReceiver;
    private readonly SocketSender _socketSender;
    private readonly Channel<ByteBuf> _sendChannel;
    private readonly IDuplexPipe _applicationPipe;
    private readonly IDuplexPipe _transportPipe;

    private readonly CancellationTokenSource _closeCts;
    private bool _socketDisposed;
    private int _doWorking;

    private Task? _receiveTask;
    private Task? _sendTask;

    public IOQueue Scheduler { get; }

    public bool IsAccept { get; }

    public IPEndPoint RemoteEndPoint { get; }

    public CancellationToken ClosedToken => _closeCts.Token;

    public PipeWriter Output => _transportPipe.Output;

    /// <summary>
    /// 总发送字节数
    /// </summary>
    public long TotalSendBytes { get; private set; }

    /// <summary>
    /// 总接收字节数
    /// </summary>
    public long TotalReceiveBytes { get; private set; }

    public uint NetId { get; set; }

    public object? UserData { get; set; }

    public object? UserData2 { get; set; }

    public IConnHandler Handler { get; set; }

    private TcpConn(IOQueue scheduler)
    {
        Scheduler = scheduler;

        _socketSender = new SocketSender(scheduler);
        _socketReceiver = new SocketReceiver(scheduler);

        _closeCts = new CancellationTokenSource();

        // 全部都在一个调度器中执行
        PipeOptions inputOptions = new PipeOptions(PinnedBlockMemoryPool.Default, scheduler, scheduler,
            useSynchronizationContext: false);
        PipeOptions outputOptions = new PipeOptions(PinnedBlockMemoryPool.Default, scheduler, scheduler,
            useSynchronizationContext: false);

        var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);
        this._transportPipe = pair.Transport;
        this._applicationPipe = pair.Application;
        _sendChannel = Channel.CreateSingleConsumerUnbounded<ByteBuf>();
    }

    internal TcpConn(Socket socket, IOQueue scheduler) : this(scheduler)
    {
        _socket = socket;
        _socket.NoDelay = true;

        IsAccept = true;
        RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint!;
        // scheduler.Schedule(_ => OnConnected(), null);
    }

    /// <summary>
    /// 用于连接服务端
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="scheduler"></param>
    public TcpConn(IPEndPoint ip, IOQueue scheduler) : this(scheduler)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.NoDelay = true;

        IsAccept = false;
        RemoteEndPoint = ip;
    }

    /// <summary>
    /// 异步连接
    /// </summary>
    public async ValueTask ConnectAsync()
    {
        SocketOperationResult result = await _socketSender.ConnectAsync(_socket, RemoteEndPoint);
        _socketSender.RemoteEndPoint = null;
        if (result.SocketError != null)
        {
            throw result.SocketError;
        }
    }

    /// <summary>
    /// 开始工作(线程安全)
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        if (Interlocked.CompareExchange(ref _doWorking, 1, 0) != 0)
        {
            throw new InvalidOperationException("Start() can only be called once");
        }

        // 工作在同个调度器中
        this.Scheduler.Schedule(static state =>
        {
            TcpConn self = (TcpConn)state!;
            self.StartInternal();
        }, this);
    }

    private void StartInternal()
    {
        Handler?.OnConnected(this);
        _receiveTask ??= ReceiveLoop();
        _sendTask ??= SendLoop();
    }

    private async Task ReceiveLoop()
    {
        var writer = _applicationPipe.Output;
        var reader = _transportPipe.Input;

        await Task.Yield();

        try
        {
            while (true)
            {
                Memory<byte> buffer = writer.GetMemory();
                SocketOperationResult result = await _socketReceiver.ReceiveAsync(_socket, buffer);
                if (!IsNormalCompletion(result))
                    break;

                if (result.BytesTransferred == 0) // FIN
                    break;

                this.TotalReceiveBytes = result.BytesTransferred;
                writer.Advance(result.BytesTransferred);

                FlushResult flushResult = await writer.FlushAsync(); // 上层已经不读了
                if (flushResult.IsCanceled || flushResult.IsCompleted)
                    break;

                // 直接开始读取解析数据
                if (!reader.TryRead(out var readResult) || readResult.IsCanceled || readResult.IsCompleted)
                    break;

                var dataBuffer = readResult.Buffer;
                Handler?.OnRead(this, ref dataBuffer);
                reader.AdvanceTo(dataBuffer.Start, dataBuffer.End);
            }
        }
        catch (Exception e)
        {
            OnError(e);
        }
        finally
        {
            try
            {
                Handler?.OnDisconnected(this); // 在接收的地方进行调用
            }
            finally
            {
                this.Dispose();
            }
        }

        writer.Complete();
        reader.Complete();

        // Console.WriteLine("receive loop end");
    }


    private bool IsNormalCompletion(SocketOperationResult result)
    {
        if (_socketDisposed)
            return false;
        if (result.SocketError == null)
            return true;
        return false;
    }

    private async Task SendLoop()
    {
        ChannelReader<ByteBuf> reader = _sendChannel.Reader;
        var output = _transportPipe.Output;
        var input = _applicationPipe.Input;

        await Task.Yield();

        while (true)
        {
            bool more = await reader.WaitToReadAsync();
            if (!more)
                break;

            while (reader.TryRead(out var buffer))
            {
                try
                {
                    Handler?.OnWrite(this, buffer, output);
                }
                finally
                {
                    buffer.Release();
                }
            }

            FlushResult flushResult = await output.FlushAsync();
            if (flushResult.IsCanceled || flushResult.IsCompleted)
                break;

            // 直接开始发送数据
            if (!input.TryRead(out var readResult))
                break;

            var data = readResult.Buffer;
            var end = data.End;
            bool isCompleted = readResult.IsCompleted;

            if (!data.IsEmpty)
            {
                SocketOperationResult result = await _socketSender.SendAsync(_socket, data);
                if (!IsNormalCompletion(result))
                    break;
                TotalSendBytes = result.BytesTransferred;
            }

            input.AdvanceTo(end); // 直接消耗完
            //Console.WriteLine($"发送数据: {this.NetId} {result.BytesTransferred} {data.End}");
            if (isCompleted)
            {
                break;
            }
        }

        output.Complete();
        input.Complete();

        // Console.WriteLine("send loop end");
    }

    private void OnError(Exception e)
    {
        this.Dispose();
    }

    /// <summary>
    /// 会给ByteBuf增加一次引用计数，在实际发送后会自动释放一次
    /// </summary>
    /// <param name="buf"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool Send(ByteBuf buf)
    {
        if (buf.Length == 0)
            throw new ArgumentException("buffer is empty");

        buf.Retain();
        return _sendChannel.Writer.TryWrite(buf);
    }

    public async Task CloseAsync()
    {
        if (!_sendChannel.Writer.TryComplete())
            return;

        // 等待发送
        if (_sendTask != null)
            await _sendTask;

        this.Dispose();
    }

    public void Dispose()
    {
        lock (this)
        {
            if (_socketDisposed)
                return;
            _socketDisposed = true;
        }

        _sendChannel.Writer.TryComplete();
        // Console.WriteLine($"disconnect : {RemoteEndPoint}");

        try
        {
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
        }
        finally
        {
            _closeCts.Cancel();
            _closeCts.Dispose();

            _socket.Close();
            _socketReceiver.Dispose();
            _socketSender.Dispose();
        }
    }
}