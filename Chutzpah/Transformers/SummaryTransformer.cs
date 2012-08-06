using System;
using System.IO;
using Chutzpah.Models;

namespace Chutzpah.Transformers
{
    /// <summary>
    /// Transforms TestCaseSummary into file or string
    /// </summary>
    public abstract class SummaryTransformer
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Transform(TestCaseSummary testFileSummary);

        public  void Transform(TestCaseSummary testFileSummary, string outFile)
        {
            if (testFileSummary == null) throw new ArgumentNullException("testFileSummary");
            if(string.IsNullOrEmpty(outFile)) throw new ArgumentNullException("outFile");

            var result = Transform(testFileSummary);
            File.WriteAllText(outFile, result);
        }
    }
}