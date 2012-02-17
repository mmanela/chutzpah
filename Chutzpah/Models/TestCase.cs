namespace Chutzpah.Models
{
    public class TestCase
    {
        public string HtmlTestFile { get; set; }
        public string InputTestFile { get; set; }
        public string ModuleName { get; set; }
        public string TestName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}