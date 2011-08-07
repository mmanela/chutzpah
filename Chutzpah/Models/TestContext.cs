using System.Collections.Generic;
using System.Collections;
namespace Chutzpah.Models
{
    public class TestContext
    {
        public TestContext()
        {
            ReferencedJavaScriptFiles = new List<ReferencedFile>();
        }

        public string InputTestFile { get; set; }
        public string TestHarnessPath { get; set; }
        public IEnumerable<ReferencedFile> ReferencedJavaScriptFiles { get; set; }
    }
}