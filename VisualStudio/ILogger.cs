using System;

namespace Chutzpah.VS.Common
{
    public enum LogType
    {
        Information,
        Warning,
        Error
    }

    public interface ILogger
    {
        void Log(string message, string source, LogType logType);
        void Log(string message, string source, Exception e);
        void MessageBox(string title, string message, LogType logType);
    }
}