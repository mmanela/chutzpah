using System;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface IProcessHelper
    {
        void LaunchLocalFileInBrowser(string file);
        void LaunchFileInBrowser(TestContext testContext, string file, string browserName = null, IDictionary<string, string> browserArgs = null);
        ProcessResult<TestCaseStreamReadResult> RunExecutableAndProcessOutput(string exePath, string arguments, Func<ProcessStreamStringSource, TestCaseStreamReadResult> streamProcessor, int streamTimeout);
        BatchCompileResult RunBatchCompileProcess(BatchCompileConfiguration compileConfiguration);
    }
}