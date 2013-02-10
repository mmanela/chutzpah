using System;
using System.Reflection;

namespace Chutzpah.Facts.Integration
{
    internal class ExceptionThrowingRunnerCallback : RunnerCallback
    {
        public override void ExceptionThrown(Exception exception, string fileName)
        {
            var preserveStackTrace = typeof(Exception).GetMethod("InternalPreserveStackTrace",
                                                                 BindingFlags.Instance | BindingFlags.NonPublic);
            preserveStackTrace.Invoke(exception, null);
            throw exception;
        }
    }
}