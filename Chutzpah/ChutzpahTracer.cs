using System;
using System.Diagnostics;
using System.Threading;

namespace Chutzpah
{
    public class ChutzpahTracer
    {
        public static void AddConsoleListener()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        public static void AddFileListener()
        {
            var listener = new DefaultTraceListener();
            listener.Filter = new EventTypeFilter(SourceLevels.All);
            listener.LogFileName = Constants.LogFileName;
            Trace.Listeners.Add(listener);
        }

        public static void TraceInformation(string messageFormat, params object[] args)
        {
            if (Trace.Listeners.Count <= 0) return;

            var message = BuildTraceMessage(messageFormat, args);
            Trace.TraceInformation(message);
        }

        public static void TraceWarning(string messageFormat, params object[] args)
        {
            if (Trace.Listeners.Count <= 0) return;

            var message = BuildTraceMessage(messageFormat, args);
            Trace.TraceWarning(message);
        }

        public static void TraceError(string messageFormat, params object[] args)
        {
            if (Trace.Listeners.Count <= 0) return;

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