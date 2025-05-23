namespace N3;

public interface IMessage
{
    public int MsgId { get; }
}

public interface IRequest : IMessage
{
    public int RpcId { get; set; }
}

public interface IResponse : IMessage
{
    public int RpcId { get; set; }
    public int ErrCode { get; set; }
    public string ErrMsg { get; set; }
}