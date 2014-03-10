using System;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileProcessors
{
    public class MochaLineNumberProcessor : LineNumberProcessor, IMochaReferencedFileProcessor
    {


        public MochaLineNumberProcessor(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {

        }

        public override Regex GetTestPattern(ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings)
        {
            var mochaFrameworkDefinition = MochaDefinition.GetInterfaceType(settings, referencedFile.Path, testFileText);
            var isCoffeeFile = referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
            switch (mochaFrameworkDefinition)
            {
                case Constants.MochaQunitInterface:

                    return isCoffeeFile ? RegexPatterns.MochaTddOrQunitTestRegexCoffeeScript : RegexPatterns.MochaTddOrQunitTestRegexJavaScript;

                case Constants.MochaBddInterface:

                    return isCoffeeFile ? RegexPatterns.MochaBddTestRegexCoffeeScript : RegexPatterns.MochaBddTestRegexJavaScript;

                case Constants.MochaTddInterface:

                    return isCoffeeFile ? RegexPatterns.MochaTddOrQunitTestRegexCoffeeScript : RegexPatterns.MochaTddOrQunitTestRegexJavaScript;

                case Constants.MochaExportsInterface:

                    return isCoffeeFile ? RegexPatterns.MochaExportsTestRegexCoffeeScript : RegexPatterns.MochaExportsTestRegexJavaScript;

                default:
                    return isCoffeeFile ? RegexPatterns.MochaBddTestRegexCoffeeScript : RegexPatterns.MochaBddTestRegexJavaScript;
            }
        }
    }
}
