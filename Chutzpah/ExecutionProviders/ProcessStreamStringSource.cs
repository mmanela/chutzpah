using System.IO;
using Chutzpah.Wrappers;
using System.Threading.Tasks;
using System;

namespace Chutzpah.Models
{
    public class ProcessStreamStringSource : TestCaseSource<string>
    {
        private readonly IProcessWrapper process;

        private StreamReader streamReader { get; }

        public ProcessStreamStringSource(IProcessWrapper process, StreamReader streamReader, int timeout) : base(timeout)
        {
            this.streamReader = streamReader;
            this.process = process;
        }

        public override void Dispose()
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Suppress any exception from here since there is nothing more we can do
            }
        }

        public override async Task<object> Open()
        {
            string line = null;
            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                Emit(line);
            }

            return null;
        }
    }
}