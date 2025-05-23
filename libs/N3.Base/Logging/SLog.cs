using System;

namespace N3
{
    /// <summary>
    /// 全局日志
    /// </summary>
    public static class SLog
    {
        private static ILogger _logger;
        internal static Func<string, ILogger> _factory;

        public static void Init(Func<string, ILogger> factory, string globalLoggerName)
        {
            _logger = factory(globalLoggerName);
            _factory = factory;
        }

        public static void Debug(string msg) => _logger.Debug(msg);
        public static void Info(string msg) => _logger.Info(msg);
        public static void Warn(string msg) => _logger.Warn(msg);
        public static void Error(string msg) => _logger.Error(msg);
        public static void Error(Exception ex, string? msg = null) => _logger.Error(ex, msg);
    }
}