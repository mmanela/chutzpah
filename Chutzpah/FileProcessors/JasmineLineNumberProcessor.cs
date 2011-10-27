namespace Chutzpah.FileProcessors
{
    using Chutzpah.Models;
    using Chutzpah.Wrappers;

    public class JasmineLineNumberProcessor : IJasmineReferencedFileProcessor
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

            var lines = this.fileSystem.GetLines(referencedFile.StagedPath);
            int lineNum = 1;

            foreach (var line in lines)
            {
                var match = RegexPatterns.JasmineTestRegex.Match(line);

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
