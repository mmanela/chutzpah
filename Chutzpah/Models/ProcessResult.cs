namespace Chutzpah.Models
{
    public class ProcessResult<T>
    {
        public ProcessResult(){}

        public ProcessResult(int exitCode)
        {
            ExitCode = exitCode;
        }

        public ProcessResult(int exitCode, T result) : this(exitCode)
        {
            Model = result;
        }

        public int ExitCode { get; set; }
        public T Model { get; set; }
    }
}