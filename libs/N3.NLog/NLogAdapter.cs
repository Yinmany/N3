using NLog.LayoutRenderers;
namespace NLog;

public class NLogAdapter : N3.ILogger
{
    private Logger _logger;

    static NLogAdapter()
    {
        LogManager.Setup().SetupExtensions(s =>
        {
            s.RegisterLayoutRenderer<ColoredConsoleLayout>();
        });
    }

    public NLogAdapter(string name)
    {
        _logger = LogManager.GetLogger(name);
    }

    public void Debug(string msg)
    {
        _logger.Debug(msg);
    }

    public void Info(string msg)
    {
        _logger.Info(msg);
    }

    public void Warn(string msg)
    {
        _logger.Warn(msg);
    }

    public void Error(string msg)
    {
        _logger.Error(msg);
    }

    public void Error(Exception ex, string msg)
    {
        _logger.Error(ex, msg);
    }
}