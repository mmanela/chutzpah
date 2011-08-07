using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using System.Text.RegularExpressions;

namespace Chutzpah.FileProcessors
{
    /// <summary>
    /// Reads a QUnit test file and determines the line number of each test
    /// </summary>
    public class QUnitLineNumberProcessor : IReferencedFileProcessor
    {
        private static Regex testAndModuleRegex = new Regex(@"(module[\t ]*\([\t ]*[""'](?<Module>.*?)[""'])|(test[\t ]*\([\t ]*[""'](?<Test>.*?)[""'])", RegexOptions.Compiled);
        IFileSystemWrapper fileSystem;
        public QUnitLineNumberProcessor(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public QUnitLineNumberProcessor()
            : this(new FileSystemWrapper())
        {
            
        }

        public void Process(ReferencedFile referencedFile)
        {
            if(!referencedFile.IsFileUnderTest) return;

            string currentModuleName = "";
            var lines = fileSystem.GetLines(referencedFile.StagedPath);
            int lineNum = 1;
            foreach (var line in lines)
            {
                var match = testAndModuleRegex.Match(line);
                while (match.Success)
                {
                    var moduleName = match.Groups["Module"].Value;
                    var testName = match.Groups["Test"].Value;
                    if (!String.IsNullOrWhiteSpace(moduleName))
                    {
                        currentModuleName = moduleName;
                    }
                    else if(!String.IsNullOrWhiteSpace(testName))
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
