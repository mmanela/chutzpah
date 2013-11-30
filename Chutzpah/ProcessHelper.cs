using System;
using System.Diagnostics;
using System.Text;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class ProcessHelper : IProcessHelper
    {
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

        public void LaunchFileInBrowser(string file)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.Verb = "Open";
            startInfo.FileName = file;
            Process.Start(startInfo);
        }
    }
}