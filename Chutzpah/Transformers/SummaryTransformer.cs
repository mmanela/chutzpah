using System;
using System.IO;
using System.Text;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.Transformers
{
    /// <summary>
    /// Transforms TestCaseSummary into file or string
    /// </summary>
    public abstract class SummaryTransformer
    {
        /// <summary>
        /// The encoding used to write the string response from Transform.
        /// </summary>
        public virtual Encoding Encoding {
            get { return Encoding.UTF8; }
        }
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
            fileSystem.WriteAllText(outFile, result,Encoding);
        }
    }
}