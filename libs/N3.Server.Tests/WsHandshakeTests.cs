using System.Text;
using System.Buffers;
using N3.Network;
using N3;

namespace N3.Tests;

public class WsHandshakeTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        string req = """
            GET /chat HTTP/1.1
            Host: server.example.com
            Upgrade: websocket
            Connection: Upgrade
            Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
            Sec-WebSocket-Version: 13


            """;


        byte[] bytes = Encoding.UTF8.GetBytes(req);
        var buffer = new ReadOnlySequence<byte>(bytes);
        WsHandshake.TryParser(ref buffer, out var protocol, out var ack);

        byte[] b = new byte[1024];
        int len = ack.Read(b, 0, (int)ack.Length);
        string t = Encoding.UTF8.GetString(b, 0, len);

        Console.WriteLine(t);
    }

    [Test]
    public void Test2()
    {
        byte[] bytes = BitConverter.GetBytes((ushort)1);
        string str = Base32.Encode(bytes);
        Console.WriteLine(str);
    }
}