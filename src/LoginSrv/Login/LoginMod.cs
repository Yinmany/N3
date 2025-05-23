
using N3;

namespace ProjectX.Login;

public class LoginMod : IAsyncDisposable
{
    private readonly SLogger _logger = new(nameof(LoginMod));

    public LoginMod()
    {
        _logger.Info("初始化...");
    }

    public async ValueTask DisposeAsync()
    {
        _logger.Info("释放...");
        await Task.Delay(1000);
    }
}