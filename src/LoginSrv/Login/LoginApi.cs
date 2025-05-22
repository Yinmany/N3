using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ystx2.Login;

[ApiController]
[Route("api/[action]")]
public class LoginApi : ControllerBase
{
    private readonly LoginMod _loginMod;
    private readonly IOptionsSnapshot<ServerList> _serverList;
    public LoginApi(LoginMod loginMod, IOptionsSnapshot<ServerList> serverList)
    {
        _loginMod = loginMod;
        _serverList = serverList;
    }

    /// <summary>
    /// 根据渠道获取服务器信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [HttpGet]
    public HttpGetServerInfoRsp GetServerInfo(string channel)
    {
        HttpGetServerInfoRsp rsp = new HttpGetServerInfoRsp();
        ServerItem item = null;
        if (!_serverList.Value.TryGetValue(channel, out item))
        {
            _serverList.Value.TryGetValue("default", out item);
        }

        if (item != null)
        {
            rsp.LoginServer = item.LoginServer;
            rsp.ResAddr = item.ResAddr;
            rsp.ResAddr2 = item.ResAddr2;
        }

        return rsp;
    }

    /// <summary>
    /// 开发时使用，可以获取所有服务器信息
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IEnumerable<HttpGetServerInfoRsp> GetServerInfo2()
    {
        return _serverList.Value.Select(f => new HttpGetServerInfoRsp
        {
            LoginServer = f.Value.LoginServer,
            ResAddr = f.Value.ResAddr,
            ResAddr2 = f.Value.ResAddr2
        });
    }
}