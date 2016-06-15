using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public ProcessResult<T> RunExecutableAndProcessOutput<T>(string exePath, string arguments, Func<ProcessStream, T> streamProcessor) where T : class
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.Start();

            ChutzpahTracer.TraceInformation("Started headless browser: {0} with PID: {1} using args: {2}", exePath, p.Id, arguments);

            // Output will be null if the stream reading times out
            var processStream = new ProcessStream(new ProcessWrapper(p), p.StandardOutput);
            var output = streamProcessor(processStream);
            p.WaitForExit(5000);



            ChutzpahTracer.TraceInformation("Ended headless browser: {0} with PID: {1} using args: {2}", exePath, p.Id, arguments);

            return new ProcessResult<T>(processStream.TimedOut ? (int)TestProcessExitCode.Timeout : p.ExitCode, output);
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