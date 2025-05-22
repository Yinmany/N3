using System.Net.Sockets;

namespace N3Lib.Network;

internal readonly struct SocketOperationResult
{
    public readonly SocketException? SocketError;
    public readonly int              BytesTransferred;

    public SocketOperationResult(int bytesTransferred)
    {
        SocketError      = null;
        BytesTransferred = bytesTransferred;
    }

    public SocketOperationResult(SocketException exception)
    {
        SocketError      = exception;
        BytesTransferred = 0;
    }
}
