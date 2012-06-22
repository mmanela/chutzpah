namespace Chutzpah.Models
{
    public class TestResult
    {
        public bool Passed { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string Message { get; set; }
    }
}