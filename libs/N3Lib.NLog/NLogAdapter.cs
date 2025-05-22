using NLog;
using NLog.LayoutRenderers;
namespace N3Lib;

public class NLogAdapter : ILogger
{
    private Logger _logger;

    static NLogAdapter()
    {
        LogManager.Setup().SetupExtensions(s => { s.RegisterLayoutRenderer<ColoredConsoleLayout>(); });
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