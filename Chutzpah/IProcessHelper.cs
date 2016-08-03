using System;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface IProcessHelper
    {
        void LaunchLocalFileInBrowser(string file);
        void LaunchFileInBrowser(TestContext testContext, string file, string browserName = null, IDictionary<string, string> browserArgs = null);
        ProcessResult<T> RunExecutableAndProcessOutput<T>(string exePath, string arguments, Func<ProcessStream, T> streamProcessor) where T : class;
        BatchCompileResult RunBatchCompileProcess(BatchCompileConfiguration compileConfiguration);
    }
}