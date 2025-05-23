using System.IO.Pipelines;

namespace N3.Network;

public class DuplexPipe : IDuplexPipe
{
    public PipeReader Input { get; }
    public PipeWriter Output { get; }

    public DuplexPipe(PipeReader reader, PipeWriter writer)
    {
        Input = reader;
        Output = writer;
    }

    /// <summary>
    /// 创建连接器的双工管道
    /// </summary>
    /// <param name="inputOptions">相对于网络层的输入</param>
    /// <param name="outputOptions">相对于网络层的输出</param>
    /// <returns></returns>
    public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);
        var output = new Pipe(outputOptions);

        var transport = new DuplexPipe(input.Reader, output.Writer);
        var application = new DuplexPipe(output.Reader, input.Writer);

        return new DuplexPipePair(transport, application);
    }

    public readonly struct DuplexPipePair
    {
        /// <summary>
        /// 对传输层的读写
        /// </summary>
        public IDuplexPipe Transport { get; }

        /// <summary>
        /// 对应用层的读写
        /// </summary>
        public IDuplexPipe Application { get; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }
    }
}