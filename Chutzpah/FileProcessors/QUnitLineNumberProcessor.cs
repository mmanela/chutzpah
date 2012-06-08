using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileProcessors
{
    /// <summary>
    /// Reads a QUnit test file and determines the line number of each test
    /// </summary>
    public class QUnitLineNumberProcessor : IQUnitReferencedFileProcessor
    {
        private readonly IFileSystemWrapper fileSystem;

        public QUnitLineNumberProcessor(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void Process(ReferencedFile referencedFile)
        {
            if (!referencedFile.IsFileUnderTest)
            {
                return;
            }

            var lines = fileSystem.GetLines(referencedFile.Path);
            int lineNum = 1;

            foreach (var line in lines)
            {
                var match = RegexPatterns.QUnitTestRegex.Match(line);

                while (match.Success)
                {
                    var testName = match.Groups["Test"].Value;

                    if (!string.IsNullOrWhiteSpace(testName))
                    {
                        referencedFile.FilePositions.Add(lineNum, match.Index + 1);
                    }

                    match = match.NextMatch();
                }

                lineNum++;
            }
        }
    }
}