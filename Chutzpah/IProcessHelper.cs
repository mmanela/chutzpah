using System;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface IProcessHelper
    {
        void LaunchFileInBrowser(string file, string browserName = null);
        ProcessResult<T> RunExecutableAndProcessOutput<T>(string exePath, string arguments, Func<ProcessStream, T> streamProcessor) where T : class;
        BatchCompileResult RunBatchCompileProcess(BatchCompileConfiguration compileConfiguration);
        void LaunchScriptDebugger(string testHarnessPath);
    }
}