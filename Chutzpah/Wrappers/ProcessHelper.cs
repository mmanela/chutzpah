using System;
using System.Diagnostics;
using System.IO;
using Chutzpah.Models;

namespace Chutzpah.Wrappers
{
    public class ProcessHelper : IProcessHelper
    {
        public ProcessResult<T> RunExecutableAndProcessOutput<T>(string exePath, string arguments, Func<StreamReader,T> streamProcessor)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = arguments;
            p.Start();
            var output = streamProcessor(p.StandardOutput);
            p.WaitForExit();
            return new ProcessResult<T>(p.ExitCode, output);
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