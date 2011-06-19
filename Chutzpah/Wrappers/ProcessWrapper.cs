using System.Diagnostics;

namespace Chutzpah.Wrappers
{
    public class ProcessWrapper : IProcessWrapper
    {
        public string RunExecutableAndCaptureOutput(string exePath, string arguments)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = arguments;
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
    }
}