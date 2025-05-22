namespace Ystx2.Login;

public class ServerList : Dictionary<string, ServerItem>
{
}

public class ServerItem
{
    public string Name { get; set; }
    public string ResAddr { get; set; }
    public string ResAddr2 { get; set; }
    public string LoginServer { get; set; }
}