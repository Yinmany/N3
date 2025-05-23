using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using N3;
using Cysharp.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.IO;
using N3;

namespace N3.AspNetCore;

public class TcpChannel : ANetChannel
{
    private readonly ConnectionContext _conn;
    private readonly Channel<RecyclableMemoryStream> _sendChannel;
    public override IPEndPoint RemoteIp { get; }

    private Task? _receiveTask, _sendTask;

    public TcpChannel(uint netId, ConnectionContext conn, bool isServer)
    {
        this.IsServer = isServer;
        this._conn = conn;
        this.NetId = netId;
        this.RemoteIp = conn.RemoteEndPoint as IPEndPoint ?? new IPEndPoint(0, 0);
        _sendChannel = Channel.CreateSingleConsumerUnbounded<RecyclableMemoryStream>();
    }

    public override Task RunAsync()
    {
        _sendTask = DoSend();
        _receiveTask = DoReceive();
        return Task.WhenAll(_sendTask, _receiveTask);
    }

    public override bool Send(RecyclableMemoryStream data)
    {
        bool isOk = _sendChannel.Writer.TryWrite(data);
        if (!isOk)
            data.Dispose();
        return isOk;
    }

    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_sendChannel.Writer.TryComplete())
            return;

        if (_sendTask != null)
            await _sendTask;
        await _conn.Transport.Output.CompleteAsync();
        _conn.Abort();
    }

    public override void Dispose()
    {
        // 已经完成就返回false；否则就是true
        if (!_sendChannel.Writer.TryComplete())
            return;
        this._conn.Abort();
    }

    private async Task DoReceive()
    {
        await Task.Yield();
        var input = _conn.Transport.Input;
        try
        {
            while (true)
            {
                ReadResult readResult = await input.ReadAsync();
                if (readResult.IsCanceled || readResult.IsCompleted)
                    break;
                var buffer = readResult.Buffer;
                while (TryParse(ref buffer, out RecyclableMemoryStream? data))
                {
                    this.LastReceiveTime = STime.NowMs;
                    this.ReceivePackets += 1;
                    this.ReceiveBytes += data!.Length;
                    this.OnData?.Invoke(this, data);
                }
                
                input.AdvanceTo(buffer.Start, buffer.End);
            }
        }
        finally
        {
            this.Dispose();
        }
    }

    private bool TryParse(ref ReadOnlySequence<byte> buffer, out RecyclableMemoryStream? data)
    {
        data = null;
        if (buffer.Length < 8) // 最少要8个
            return false;
        Span<byte> tmpSpan = stackalloc byte[4];
        buffer.CopyTo(tmpSpan);
        uint bodyLen = BinaryPrimitives.ReadUInt32LittleEndian(tmpSpan);
        if (buffer.Length - 4 < bodyLen) // 数据不够
            return false;

        var bodyBuffer = buffer.Slice(4, bodyLen);
        buffer = buffer.Slice(bodyLen + 4);
        data = NetBuffer.Rent();
        if (bodyBuffer.IsSingleSegment)
        {
            data.Write(bodyBuffer.FirstSpan);
        }
        else
        {
            // 多个
            var enumerator = bodyBuffer.GetEnumerator();
            enumerator.MoveNext();
            bool end;
            do
            {
                var currentBuffer = enumerator.Current;
                end = enumerator.MoveNext(); // 没有下一个，当前就是结束消息。
                data.Write(currentBuffer.Span);
            } while (!end);
        }

        data.Seek(0, SeekOrigin.Begin);
        return true;
    }

    private void WriteLengthField(PipeWriter writer, uint len)
    {
        Span<byte> bodyLenSpan = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bodyLenSpan, len);
        writer.Write(bodyLenSpan);
    }

    private async Task DoSend()
    {
        await Task.Yield();
        ChannelReader<RecyclableMemoryStream> reader = _sendChannel.Reader;
        var output = _conn.Transport.Output;
        try
        {
            await foreach (var stream in reader.ReadAllAsync())
            {
                try
                {
                    ReadOnlySequence<byte> sendBuffer = stream.GetReadOnlySequence();

                    // 先写入4字节长度
                    uint bodyLen = (uint)sendBuffer.Length;
                    WriteLengthField(output, bodyLen);

                    if (sendBuffer.IsSingleSegment)
                    {
                        output.Write(sendBuffer.FirstSpan);
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
                            output.Write(currentBuffer.Span);
                        } while (!end);
                    }

                    FlushResult flushResult = await output.FlushAsync();
                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            SLog.Error(ex, "tcp send:");
        }
    }
}