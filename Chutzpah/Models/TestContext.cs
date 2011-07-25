using System.Collections.Generic;
namespace Chutzpah.Models
{
    public class TestContext
    {
        public string TestHarnessPath { get; set; }
        public IEnumerable<ReferencedJavaScriptFile> ReferencedJavaScriptFiles { get; set; }
    }

    public class ReferencedJavaScriptFile
    {
        public string StagedPath { get; set; }
        public string Path { get; set; }
        public bool IsLocal { get; set; }
    }
}