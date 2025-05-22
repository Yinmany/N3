using System;

namespace N3Lib
{
    public interface ILogger
    {
        void Debug(string msg);
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
        void Error(Exception ex, string msg);
    }
}