using System;
using System.Linq;

namespace Chutzpah.FrameworkDefinitions
{
    using System.Collections.Generic;
    using Chutzpah.FileProcessors;

    /// <summary>
    /// Definition that describes the Mocha framework.
    /// </summary>
    public class MochaDefinition : BaseFrameworkDefinition
    {
        private IEnumerable<IMochaReferencedFileProcessor> fileProcessors;
        private IEnumerable<string> fileDependencies;

        /// <summary>
        /// Initializes a new instance of the MochaDefinition class.
        /// </summary>
        public MochaDefinition(IEnumerable<IMochaReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;
            this.fileDependencies = new[]
                {
                    "mocha\\mocha.css", 
                    "mocha\\mocha.js", 
                };
        }

        /// <summary>
        /// Gets a list of file dependencies to bundle with the Mocha test harness.
        /// </summary>
        public override IEnumerable<string> FileDependencies
        {
            get
            {
                return this.fileDependencies;
            }
        }

        public override string TestHarness
        {
            get { return @"mocha\mocha.html"; }
        }

        /// <summary>
        /// Gets a short, file system friendly key for the Mocha library.
        /// </summary>
        public override string FrameworkKey
        {
            get
            {
                return "mocha";
            }
        }


        /// <summary>
        /// Gets a list of file processors to call within the Process method.
        /// </summary>
        protected override IEnumerable<IReferencedFileProcessor> FileProcessors
        {
            get
            {
                return this.fileProcessors;
            }
        }

        public static string GetInterfaceType(string testFilePath, string testFileText)
        {
            var isCoffeeFile = testFilePath.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);

            if (isCoffeeFile)
            {
                if (RegexPatterns.MochaBddTestRegexCoffeeScript.IsMatch(testFileText)) return "bdd";
                if (RegexPatterns.MochaTddOrQunitTestRegexCoffeeScript.IsMatch(testFileText))
                {
                    return RegexPatterns.MochaTddSuiteRegexCoffeeScript.IsMatch(testFileText) ? "tdd" : "qunit";
                }
                if (RegexPatterns.MochaExportsTestRegexCoffeeScript.IsMatch(testFileText)) return "exports";
            }
            else
            {
                if (RegexPatterns.MochaBddTestRegexJavaScript.IsMatch(testFileText)) return "bdd";
                if (RegexPatterns.MochaTddOrQunitTestRegexJavaScript.IsMatch(testFileText))
                {
                    return RegexPatterns.MochaTddSuiteRegexJavaScript.IsMatch(testFileText) ? "tdd" : "qunit";
                }
                if (RegexPatterns.MochaExportsTestRegexJavaScript.IsMatch(testFileText)) return "exports";
            }

            return "bdd";
        }

        public override IEnumerable<Tuple<string, string>> GetFrameworkReplacements(string testFilePath, string testFileText)
        {
            string interfaceType = GetInterfaceType(testFilePath, testFileText);

            return new[] { Tuple.Create("MochaUi", interfaceType) };
        }
    }
}
