using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace N3Lib.Network;

internal sealed class SocketSender : SocketAwaitableEventArgs
{
    private List<ArraySegment<byte>>? _bufferList;

    public SocketSender(IOQueue ioQueue) : base(ioQueue)
    {
    }

    public ValueTask<SocketOperationResult> ConnectAsync(Socket socket, IPEndPoint ip)
    {
        this.RemoteEndPoint = ip;
        if (socket.ConnectAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }

    public ValueTask<SocketOperationResult> SendAsync(Socket socket, in ReadOnlySequence<byte> buffers)
    {
        Reset();

        if (buffers.IsSingleSegment)
        {
            return SendAsync(socket, buffers.First);
        }

        SetBufferList(buffers);
        if (socket.SendAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }

    private ValueTask<SocketOperationResult> SendAsync(Socket socket, ReadOnlyMemory<byte> memory)
    {
        SetBuffer(MemoryMarshal.AsMemory(memory));
        if (socket.SendAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var transferred = BytesTransferred;
        var error = SocketError;
        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(transferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }

    private void SetBufferList(in ReadOnlySequence<byte> buffer)
    {
        Debug.Assert(!buffer.IsEmpty);
        Debug.Assert(!buffer.IsSingleSegment);

        if (_bufferList == null)
        {
            _bufferList = new List<ArraySegment<byte>>();
        }

        foreach (var b in buffer)
        {
            if (!MemoryMarshal.TryGetArray(b, out var array))
            {
                throw new InvalidOperationException("Buffer is not backed by an array.");
            }

            _bufferList.Add(array);
        }

        // The act of setting this list, sets the buffers in the internal buffer list
        BufferList = _bufferList;
    }

    public void Reset()
    {
        // We clear the buffer and buffer list before we put it back into the pool
        // it's a small performance hit but it removes the confusion when looking at dumps to see this still
        // holds onto the buffer when it's back in the pool
        if (BufferList != null)
        {
            BufferList = null;

            _bufferList?.Clear();
        }
        else
        {
            SetBuffer(null, 0, 0);
        }

        this.RemoteEndPoint = null;
    }
}