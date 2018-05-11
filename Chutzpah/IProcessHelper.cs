using System;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface IProcessHelper
    {
        void LaunchLocalFileInBrowser(string file);
        void LaunchFileInBrowser(TestContext testContext, string file, string browserName = null, IDictionary<string, string> browserArgs = null);
        ProcessResult<TestCaseStreamReadResult> RunExecutableAndProcessOutput(string exePath, string arguments, Func<ProcessStreamStringSource, TestCaseStreamReadResult> streamProcessor, int streamTimeout, IDictionary<string, string> environmentVars);
        bool RunExecutableAndProcessOutput(string exePath, string arguments, IDictionary<string, string> environmentVars, out string standardOutput, out string standardError);
        BatchCompileResult RunBatchCompileProcess(BatchCompileConfiguration compileConfiguration);
        bool IsRunningElevated();
    }
}