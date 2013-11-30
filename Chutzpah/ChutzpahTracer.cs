using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Chutzpah
{
    public class ChutzpahTracer
    {
        public static bool Enabled { get; set; }
        
        static ChutzpahTracer()
        {
            Enabled = true;
        }

        public static void AddConsoleListener()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        public static void AddFileListener(string path = null)
        {
            var logPath = string.IsNullOrEmpty(path) ? Constants.LogFileName : path;
        
            if(Trace.Listeners[logPath] == null)
            {
                var listener = new DefaultTraceListener();
                listener.Filter = new EventTypeFilter(SourceLevels.All);
                listener.LogFileName = logPath;
                listener.Name = logPath;
                Trace.Listeners.Add(listener);
            }
        }

        public static void RemoveFileListener(string path = null)
        {
            var logPath = string.IsNullOrEmpty(path) ? Constants.LogFileName : path;
            Trace.Listeners.Remove(logPath);
        }

        public static void TraceInformation(string messageFormat, params object[] args)
        {
            if (!Enabled || Trace.Listeners.Count <= 0) return;

            var message = BuildTraceMessage(messageFormat, args);
            Trace.TraceInformation(message);
        }

        public static void TraceWarning(string messageFormat, params object[] args)
        {
            if (!Enabled || Trace.Listeners.Count <= 0) return;

            var message = BuildTraceMessage(messageFormat, args);
            Trace.TraceWarning(message);
        }

        public static void TraceError(string messageFormat, params object[] args)
        {
            if (!Enabled || Trace.Listeners.Count <= 0) return;

            var message = BuildTraceMessage(messageFormat, args);
            Trace.TraceError(message);
        }

        public static void TraceError(Exception exception, string messageFormat, params object[] args)
        {
            if (Trace.Listeners.Count <= 0) return;

            var message = BuildTraceMessage(messageFormat, exception, args);
            Trace.TraceError(message);
        }

        private static string BuildTraceMessage(string innerMessageFormat, object[] args = null)
        {
            return BuildTraceMessage(innerMessageFormat, null, args);
        }

        private static string BuildTraceMessage(string innerMessageFormat, Exception exception, object[] args = null)
        {
            var innerMessage = args != null && args.Length > 0 ? string.Format(innerMessageFormat, args) : innerMessageFormat;

            var messageFormat = exception == null ? "Time:{0}; Thread:{1}; Message:{2}"
                                                  : "Time:{0}; Thread:{1}; Message:{2}, Exception:{3}";

            var message = string.Format(messageFormat,
                                        DateTime.Now.TimeOfDay,
                                        Thread.CurrentThread.ManagedThreadId,
                                        innerMessage,
                                        exception == null ? "" : exception.ToString());
            return message;
        }
    }
}