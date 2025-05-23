using System;

namespace N3
{
    public readonly struct SLogger
    {
        private readonly ILogger _logger;

        public SLogger(string name)
        {
            _logger = SLog._factory(name);
        }

        public static implicit operator SLogger(string name)
        {
            SLogger l = new SLogger(name);
            return l;
        }

        public void Debug(string msg) => _logger.Debug(msg);
        public void Info(string msg) => _logger.Info(msg);
        public void Warn(string msg) => _logger.Warn(msg);
        public void Error(string msg) => _logger.Error(msg);
        public void Error(Exception ex, string? msg = null) => _logger.Error(ex, msg);
    }
}