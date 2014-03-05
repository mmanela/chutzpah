namespace Chutzpah.Models
{
    public class BatchCompileResult
    {
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public int ExitCode { get; set; }

    }
}