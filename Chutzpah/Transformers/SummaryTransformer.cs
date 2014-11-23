using System;
using System.IO;
using Chutzpah.Models;
using Chutzpah.Wrappers;

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

        private readonly IFileSystemWrapper fileSystem;

        public SummaryTransformer(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public virtual void Transform(TestCaseSummary testFileSummary, string outFile)
        {
            if (testFileSummary == null)
            {
                throw new ArgumentNullException("testFileSummary");
            }
            else if (string.IsNullOrEmpty(outFile))
            {
                throw new ArgumentNullException("outFile");
            }

            var result = Transform(testFileSummary);
            fileSystem.WriteAllText(outFile, result);
        }
    }
}