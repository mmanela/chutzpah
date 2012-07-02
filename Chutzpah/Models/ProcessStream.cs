using System.Diagnostics;
using System.IO;
using Chutzpah.Wrappers;

namespace Chutzpah.Models
{
    public class ProcessStream
    {
        private readonly IProcessWrapper process;

        public StreamReader StreamReader { get; private set; }

        public ProcessStream(IProcessWrapper process, StreamReader streamReader)
        {
            StreamReader = streamReader;
            this.process = process;
        }

        public void KillProcess()
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Supress any exception from here since there is nothing more we can do
            }
        }
    }
}