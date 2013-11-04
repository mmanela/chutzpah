using System;
using System.Linq;

namespace Chutzpah.FileProcessors
{
    using Chutzpah.Models;
    using Chutzpah.Wrappers;

    public class MochaLineNumberProcessor : IMochaReferencedFileProcessor
    {
        private IFileSystemWrapper fileSystem;

        public MochaLineNumberProcessor(IFileSystemWrapper fileSystem)
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
            
            var lines = this.fileSystem.GetLines(referencedFile.Path);
            
            var contents = string.Join(Environment.NewLine, lines);

            var coffeeScriptTestPatterns = new[] {
                RegexPatterns.MochaBddTestRegexCoffeeScript,
                RegexPatterns.MochaTddOrQunitTestRegexCoffeeScript,
                RegexPatterns.MochaExportsTestRegexCoffeeScript
            };

            var javaScriptTestPatterns = new[] {
                RegexPatterns.MochaBddTestRegexJavaScript,
                RegexPatterns.MochaTddOrQunitTestRegexJavaScript,
                RegexPatterns.MochaExportsTestRegexJavaScript
            };

            var patterns = isCoffeeFile ? coffeeScriptTestPatterns : javaScriptTestPatterns;

            var regExp = patterns.FirstOrDefault(p => p.IsMatch(contents));

            if (regExp == null)
                return;

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
