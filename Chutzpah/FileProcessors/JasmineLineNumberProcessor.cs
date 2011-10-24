namespace Chutzpah.FileProcessors
{
    using System;
    using Chutzpah.Models;
    using Chutzpah.Wrappers;

    public class JasmineLineNumberProcessor : IReferencedFileProcessor
    {
        private IFileSystemWrapper fileSystem;

        public JasmineLineNumberProcessor(IFileSystemWrapper fileSystem)
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
            var lines = fileSystem.GetLines(referencedFile.StagedPath);
            int lineNum = 1;

            foreach (var line in lines)
            {
                var match = RegexPatterns.JasmineTestAndModuleRegex.Match(line);

                while (match.Success)
                {
                    var moduleName = match.Groups["Module"].Value;
                    var testName = match.Groups["Test"].Value;
                    if (!String.IsNullOrWhiteSpace(moduleName))
                    {
                        currentModuleName = moduleName;
                    }
                    else if (!String.IsNullOrWhiteSpace(testName))
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
