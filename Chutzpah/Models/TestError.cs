using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class TestError 
    {
        public TestError()
        {
            Stack = new List<Stack>();
        }

        public string InputTestFile { get; set; }
        public string Message { get; set; }
        public IList<Stack> Stack { get; set; }
    }
}