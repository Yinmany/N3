namespace N3;

internal class RpcErrorCode
{
    public const int NotFoundTarget = -1; // 目标不存在
    public const int Exception = -2; // 异常
    public const int NotFoundHandler = -3; // 消息处理器不存在
    public const int ErrorNotFoundPid = -4; // 找不到目标进程
    public const int ErrorTimeout = -5; // 请求超时
    public const int ErrorDisconnect = -6; // 连接已断开
    public const int ErrorDuplicateRpcId = -7; // rpcId重复
}

public class RpcException : Exception
{
    public static readonly RpcException NotFoundNode = new(RpcErrorCode.ErrorNotFoundPid, "找不到目标节点.");

    /// <summary>
    /// 连接已断开
    /// </summary>
    public static readonly RpcException Disconnect = new(RpcErrorCode.ErrorDisconnect, "连接已断开.");

    public static readonly RpcException Timeout = new(RpcErrorCode.ErrorTimeout, "请求超时.");

    // rpcId重复
    public static readonly RpcException DuplicateRpcId = new(RpcErrorCode.ErrorDuplicateRpcId, "rpcId重复.");

    public int ErrorCode { get; private set; }

    public RpcException(int errorCode)
    {
        this.ErrorCode = errorCode;
    }

    public RpcException(int errorCode, string msg) : base(msg)
    {
        this.ErrorCode = errorCode;
    }
}