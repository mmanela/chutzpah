namespace Chutzpah.Models
{
    public class JsonTestCase
    {
        public bool Passed { get; set; }
        public string Name { get; set; }
        public string Module { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string Message { get; set; }
    }
}