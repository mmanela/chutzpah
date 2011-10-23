namespace Chutzpah.Models
{
    using System.Collections.Generic;
    using Chutzpah.FrameworkDefinitions;

    public class TestContext
    {
        public TestContext()
        {
            this.ReferencedJavaScriptFiles = new List<ReferencedFile>();
        }

        public string InputTestFile { get; set; }

        public string TestRunner { get; set; }

        public string TestHarnessPath { get; set; }

        public IFrameworkDefinition FrameworkDefinition { get; set; }

        public IEnumerable<ReferencedFile> ReferencedJavaScriptFiles { get; set; }
    }
}