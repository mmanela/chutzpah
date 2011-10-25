namespace Chutzpah.FileProcessors
{
    using Chutzpah.Models;
    using Chutzpah.Wrappers;

    /// <summary>
    /// Reads a QUnit test file and determines the line number of each test
    /// </summary>
    public class QUnitLineNumberProcessor : IQUnitReferencedFileProcessor
    {
        private IFileSystemWrapper fileSystem;

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

            string currentModuleName = string.Empty;
            var lines = this.fileSystem.GetLines(referencedFile.StagedPath);
            int lineNum = 1;

            foreach (var line in lines)
            {
                var match = RegexPatterns.QUnitTestAndModuleRegex.Match(line);

                while (match.Success)
                {
                    var moduleName = match.Groups["Module"].Value;
                    var testName = match.Groups["Test"].Value;

                    if (!string.IsNullOrWhiteSpace(moduleName))
                    {
                        currentModuleName = moduleName;
                    }
                    else if (!string.IsNullOrWhiteSpace(testName))
                    {
                        referencedFile.FilePositions.Add(currentModuleName, testName, lineNum, match.Index + 1);
                    }

                    match = match.NextMatch();
                }

                lineNum++;
            }
        }
    }
}
