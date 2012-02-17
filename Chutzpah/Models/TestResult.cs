namespace Chutzpah.Models
{
    public class TestResult : TestCase
    {
        public bool Passed { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string Message { get; set; }
    }
}