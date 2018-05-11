using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Microsoft.Win32;

namespace Chutzpah
{
    public class ProcessHelper : IProcessHelper
    {
        readonly IUrlBuilder urlBuilder;

        public ProcessHelper(IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
        }

        public ProcessResult<TestCaseStreamReadResult> RunExecutableAndProcessOutput(string exePath, string arguments, Func<ProcessStreamStringSource, TestCaseStreamReadResult> streamProcessor, int streamTimeout, IDictionary<string, string> environmentVars)
        {
            Process p = InvokeProcess(exePath, arguments, environmentVars);

            ChutzpahTracer.TraceInformation("Started headless browser: {0} with PID: {1} using args: {2}", exePath, p.Id, arguments);

            // Output will be null if the stream reading times out
            var processStream = new ProcessStreamStringSource(new ProcessWrapper(p), p.StandardOutput, streamTimeout);
            var output = streamProcessor(processStream);
            p.WaitForExit(5000);



            ChutzpahTracer.TraceInformation("Ended headless browser: {0} with PID: {1} using args: {2}", exePath, p.Id, arguments);

            return new ProcessResult<TestCaseStreamReadResult>(output.TimedOut ? (int)TestProcessExitCode.Timeout : p.ExitCode, output);
        }


        public bool RunExecutableAndProcessOutput(string exePath, string arguments, IDictionary<string, string> environmentVars, out string standardOutput, out string standardError)
        {
            Process p = InvokeProcess(exePath, arguments, environmentVars);

            ChutzpahTracer.TraceInformation("Started executable: {0} with PID: {1} using args: {2}", exePath, p.Id, arguments);


            p.WaitForExit(120 * 1000);


            standardOutput = null;
            StringBuilder output = new StringBuilder();
            while (!p.StandardOutput.EndOfStream)
            {
                string line = p.StandardOutput.ReadLine();
                output.AppendLine(line);
            }
            standardOutput = output.ToString();

            standardError = null;
            StringBuilder error = new StringBuilder();
            while (!p.StandardError.EndOfStream)
            {
                string line = p.StandardError.ReadLine();
                error.AppendLine(line);
            }
            standardError = error.ToString();

            ChutzpahTracer.TraceInformation("Ended executable: {0} with PID: {1} using args: {2}", exePath, p.Id, arguments);

            return p.ExitCode == 0;
        }


        private static Process InvokeProcess(string exePath, string arguments, IDictionary<string, string> environmentVars)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;

            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            if (environmentVars != null)
            {
                foreach (KeyValuePair<string, string> entry in environmentVars)
                {
                    p.StartInfo.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }

            p.Start();
            return p;
        }

        public BatchCompileResult RunBatchCompileProcess(BatchCompileConfiguration compileConfiguration)
        {
            ChutzpahTracer.TraceInformation("Started batch compile using {0} with args {1}", compileConfiguration.Executable, compileConfiguration.Arguments);

            var timeout = compileConfiguration.Timeout.Value;
            var p = new Process();
            // Append path to where .net drop is so you can use things like msbuild
            p.StartInfo.EnvironmentVariables["Path"] += ";" + System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() + ";";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = compileConfiguration.Executable;
            p.StartInfo.Arguments = compileConfiguration.Arguments;
            p.StartInfo.WorkingDirectory = compileConfiguration.WorkingDirectory;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;


            var output = new StringBuilder();
            var error = new StringBuilder();

            // Ensure we read both input/output fully and don't get into a deadlock
            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                p.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                p.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (p.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    ChutzpahTracer.TraceInformation("Finished batch compile with exit code {0} using {1} with args {2}", p.ExitCode, compileConfiguration.Executable, compileConfiguration.Arguments);
                }
                else
                {
                    ChutzpahTracer.TraceInformation("Finished batch compile on a timeout with exit code {0} using {1} with args {2}", p.ExitCode, compileConfiguration.Executable, compileConfiguration.Arguments);
                }

                return new BatchCompileResult { StandardError = error.ToString(), StandardOutput = output.ToString(), ExitCode = p.ExitCode };

            }

        }

        public void LaunchFileInBrowser(TestContext testContext, string file, string browserName = null, IDictionary<string, string> browserArgs = null)
        {
            file = urlBuilder.GenerateFileUrl(testContext, file, fullyQualified: true);
            OpenBrowser(file, browserName, browserArgs);
        }

        public void LaunchLocalFileInBrowser(string file)
        {
            file = urlBuilder.GenerateLocalFileUrl(file);
            OpenBrowser(file);
        }

        public bool IsRunningElevated()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void OpenBrowser(string file, string browserName = null, IDictionary<string, string> browserArgs = null)
        {
            var browserAppPath = BrowserPathHelper.GetBrowserPath(browserName);

            var startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(browserAppPath))
            {
                startInfo.UseShellExecute = true;
                startInfo.FileName = browserAppPath;
                startInfo.Arguments =
                    GetArguments(browserName ?? Path.GetFileNameWithoutExtension(browserAppPath),
                                 file,
                                 browserArgs);
            }
            else
            {
                startInfo.UseShellExecute = true;
                startInfo.Verb = "Open";
                startInfo.FileName = file;
            }

            Process.Start(startInfo);
        }

        static string GetArguments(string browser, string file, IDictionary<string, string> browserArgs)
        {
            if (string.IsNullOrWhiteSpace(browser) || browserArgs == null)
            {
                return file;
            }

            string args;
            if (browserArgs.TryGetValue(browser, out args) && !string.IsNullOrWhiteSpace(args))
            {
                return string.Format("{0} {1}", args, file);
            }

            return file;
        }
    }
}