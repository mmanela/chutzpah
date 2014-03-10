using System;
using System.Text.RegularExpressions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileProcessors
{

    public class JasmineLineNumberProcessor : LineNumberProcessor, IJasmineReferencedFileProcessor
    {
        public JasmineLineNumberProcessor(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {
        }
        public override Regex GetTestPattern(ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings)
        {
            var isCoffeeFile = referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
            var regExp = isCoffeeFile
                             ? RegexPatterns.JasmineTestRegexCoffeeScript
                             : RegexPatterns.JasmineTestRegexJavaScript;
            return regExp;
        }
    }
}
