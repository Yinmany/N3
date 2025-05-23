using System.Net;
using System.Net.Sockets;

namespace N3.Network;

public class TcpConnListener : IDisposable
{
    private readonly SocketSchedulers _socketSchedulers;
    private Socket _listenSocket;
    private bool _isDisposed;
    private readonly object _lockObject = new object();

    public TcpConnListener(SocketSchedulers socketSchedulers)
    {
        _socketSchedulers = socketSchedulers;
    }

    public void Listen(int port, IPAddress? ip = null, int backlog = 64)
    {
        if (_listenSocket != null)
            throw new Exception("已经Listen");

        IPEndPoint bindIp = new IPEndPoint(ip ?? IPAddress.Any, port);
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(bindIp);
        _listenSocket.Listen(backlog);
    }

    public async ValueTask<TcpConn?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
#if NETSTANDARD2_1_OR_GREATER
                Socket acceptSocket = await _listenSocket.AcceptAsync();

#else
                Socket acceptSocket = await _listenSocket.AcceptAsync(cancellationToken);

#endif
                TcpConn conn = new TcpConn(acceptSocket, _socketSchedulers.GetScheduler());
                return conn;
            }
            catch (ObjectDisposedException e)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException)
            {
            }
        }
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _listenSocket?.Dispose();
        }
    }
}