namespace Chutzpah.Models
{
    public class ProcessResult
    {
        public ProcessResult(){}

        public ProcessResult(int exitCode)
        {
            ExitCode = exitCode;
        }

        public ProcessResult(int exitCode, string standardOutput) : this(exitCode)
        {
            StandardOutput = standardOutput;
        }

        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
    }
}