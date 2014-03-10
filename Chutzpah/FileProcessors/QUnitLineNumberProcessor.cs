using System;
using System.Text.RegularExpressions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileProcessors
{
    /// <summary>
    /// Reads a QUnit test file and determines the line number of each test
    /// </summary>
    public class QUnitLineNumberProcessor : LineNumberProcessor, IQUnitReferencedFileProcessor
    {
        public QUnitLineNumberProcessor(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {
        }

        public override Regex GetTestPattern(ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings)
        {
            var isCoffeeFile = referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
            var regExp = isCoffeeFile
                             ? RegexPatterns.QUnitTestRegexCoffeeScript
                             : RegexPatterns.QUnitTestRegexJavaScript;
            return regExp;
        }
    }
}