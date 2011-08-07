namespace Chutzpah.Models
{
    public class TestResult
    {
        public string HtmlTestFile { get; set; }
        public string InputTestFile { get; set; }
        public string ModuleName { get; set; }
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}