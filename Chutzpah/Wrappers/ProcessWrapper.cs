using System.Diagnostics;
using System.IO;

namespace Chutzpah.Wrappers
{
    /// <summary>
    /// A testable version of the Process class
    /// </summary>
    public class ProcessWrapper : IProcessWrapper
    {
        private readonly Process process;

        public ProcessWrapper(Process process)
        {
            this.process = process;
        }

        public ProcessStartInfo StartInfo
        {
            get { return process.StartInfo; }
            set { process.StartInfo = value; }
        }

        public StreamReader StandardOutput
        {
            get { return process.StandardOutput; }
        }


        public StreamWriter StandardInput
        {
            get { return process.StandardInput; }
        }

        public StreamReader StandardError
        {
            get { return process.StandardError; }
        }

        public bool Start()
        {
            return process.Start();
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        public bool WaitForExit(int milliseconds)
        {
            return process.WaitForExit(milliseconds);
        }

        public void Kill()
        {
            process.Kill();
        }
    }
}