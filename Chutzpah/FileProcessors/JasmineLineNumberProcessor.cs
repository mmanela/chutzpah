using System;

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
            var isCoffeeFile = referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
            var regExp = isCoffeeFile
                             ? RegexPatterns.JasmineTestRegexCoffeeScript
                             : RegexPatterns.JasmineTestRegexJavaScript;

            var lines = this.fileSystem.GetLines(referencedFile.Path);
            int lineNum = 1;

            foreach (var line in lines)
            {
                var match = regExp.Match(line);

                while (match.Success)
                {
                    var testName = match.Groups["Test"].Value;

                    if (!string.IsNullOrWhiteSpace(testName))
                    {
                        var testFunc = match.Groups["Tf"];
                        referencedFile.FilePositions.Add(lineNum, testFunc.Index + 1);
                    }

                    match = match.NextMatch();
                }

                lineNum++;
            }
        }
    }
}
