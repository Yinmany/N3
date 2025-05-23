using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using Cysharp.Threading.Tasks;
using Microsoft.IO;
using N3;

namespace N3.AspNetCore;

public sealed class WebSocketChannel : ANetChannel
{
    private readonly WebSocket _socket;
    private readonly Channel<RecyclableMemoryStream> _sendChannel;
    public override IPEndPoint RemoteIp { get; }

    private Task? _sendTask;
    private Task? _receiveTask;

    public WebSocketChannel(uint netId, WebSocket socket, IPEndPoint remoteIp)
    {
        this.NetId = netId;
        _socket = socket;
        _sendChannel = Channel.CreateSingleConsumerUnbounded<RecyclableMemoryStream>();
        this.RemoteIp = remoteIp;
    }

    public override Task RunAsync()
    {
        _sendTask = DoSendAsync();
        _receiveTask = DoReceiveAsync();
        return Task.WhenAll(_sendTask, _receiveTask);
    }

    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        // 不会在发送新的数据了
        if (!_sendChannel.Writer.TryComplete())
            return;

        if (_sendTask != null)
            await _sendTask; // 等待发送channel完成

        // bad 发送关闭帧，并等待接收关闭帧响应
        // await _socket.CloseAsync()

        // good 只用发送关闭帧即可
        await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, default);
        this._socket.Abort();
    }


    private async Task DoReceiveAsync()
    {
        await Task.Yield();

        var ws = _socket;

        RecyclableMemoryStream? memoryStream = null;
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                memoryStream = NetBuffer.Rent();
                Memory<byte> buffer = memoryStream.GetMemory(NetBuffer.BlockSize);
                var result = await ws.ReceiveAsync(buffer, default).ConfigureAwait(false); // token取消后，状态会变为Abort;连接就不能使用了
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                memoryStream.Advance(result.Count);
                if (!result.EndOfMessage)
                    continue;

                memoryStream.Seek(0, SeekOrigin.Begin);

                // 由上层自行释放流
                var temp = memoryStream;
                memoryStream = null;
                OnData?.Invoke(this, temp);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
            // webSocketException.WebSocketErrorCode == WebSocketError.InvalidState
        }
        catch (Exception e)
        {
            SLog.Error(e, "ws异常:");
        }
        finally
        {
            memoryStream?.Dispose(); // 回收掉
            this.Dispose();

            // SLog.Debug("receive end");
        }
    }

    private async Task DoSendAsync()
    {
        ChannelReader<RecyclableMemoryStream> reader = _sendChannel.Reader;
        try
        {
            await foreach (var stream in reader.ReadAllAsync())
            {
                try
                {
                    ReadOnlySequence<byte> sendBuffer = stream.GetReadOnlySequence();
                    if (sendBuffer.IsSingleSegment)
                    {
                        await _socket.SendAsync(sendBuffer.First, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                    else
                    {
                        // 多个
                        var enumerator = sendBuffer.GetEnumerator();
                        enumerator.MoveNext();
                        bool end;
                        do
                        {
                            var currentBuffer = enumerator.Current;
                            end = enumerator.MoveNext(); // 没有下一个，当前就是结束消息。
                            await _socket.SendAsync(currentBuffer, WebSocketMessageType.Binary, !end, CancellationToken.None);
                        } while (!end);
                    }
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            SLog.Error(ex, "ws send:");
        }

        // SLog.Debug("send end");
    }


    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data"></param>
    /// <returns>false连接已经关闭了,将会释放掉data</returns>
    public override bool Send(RecyclableMemoryStream data)
    {
        data.Seek(0, SeekOrigin.Begin);
        bool isOk = _sendChannel.Writer.TryWrite(data);
        if (!isOk)
            data.Dispose();
        return isOk;
    }

    public override void Dispose()
    {
        // 已经完成就返回false；否则就是true
        if (!_sendChannel.Writer.TryComplete())
            return;
        this._socket.Abort();
    }
}