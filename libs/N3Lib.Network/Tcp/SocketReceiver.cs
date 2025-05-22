using System.Net.Sockets;

namespace N3Lib.Network;

internal sealed class SocketReceiver : SocketAwaitableEventArgs
{
    private short _token;

    public SocketReceiver(IOQueue ioQueue) : base(ioQueue)
    {
    }

    public ValueTask<SocketOperationResult> ReceiveAsync(Socket socket, Memory<byte> memory)
    {
        SetBuffer(memory);
        if (socket.ReceiveAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, _token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(transferred))

#if NETSTANDARD2_1_OR_GREATER
            : new ValueTask<SocketOperationResult>(Task.FromException<SocketOperationResult>(CreateException(err))); // 等于 ValueTask.FromException
#else
            : ValueTask.FromException<SocketOperationResult>(CreateException(err));
#endif
    }
}
