using System.IO;
using System.Text.RegularExpressions;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileProcessors
{
    /// <summary>
    /// Reads a test file and determines the line number of each test
    /// </summary>
    public abstract class LineNumberProcessor : IReferencedFileProcessor
    {
        private readonly IFileSystemWrapper fileSystem;

        protected LineNumberProcessor(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public abstract Regex GetTestPattern(ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings);

        public void Process(IFrameworkDefinition frameworkDefinition, ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings)
        {
            if (!referencedFile.IsFileUnderTest)
            {
                return;
            }

            var regExp = settings.TestPatternRegex ?? GetTestPattern(referencedFile, testFileText, settings);


            int lineNum = 1;
            using (var reader = new StringReader(testFileText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = regExp.Match(line);

                    while (match.Success)
                    {
                        var testNameGroup = match.Groups["TestName"];
                        var testName = testNameGroup.Value;

                        if (!string.IsNullOrWhiteSpace(testName))
                        {
                            referencedFile.FilePositions.Add(lineNum, testNameGroup.Index + 1, testName);
                        }

                        match = match.NextMatch();
                    }

                    lineNum++;
                }
            }

        }
    }
}