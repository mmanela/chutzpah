using Chutzpah.Models;

namespace Chutzpah.FrameworkDefinitions
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Chutzpah.FileProcessors;

    /// <summary>
    /// Definition that describes the QUnit framework.
    /// </summary>
    public class QUnitDefinition : BaseFrameworkDefinition
    {
        private IEnumerable<IQUnitReferencedFileProcessor> fileProcessors;
        private IDictionary<string, IEnumerable<string>> fileDependencies = new Dictionary<string, IEnumerable<string>>();
        private IDictionary<string, string> testHarness = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the QUnitDefinition class.
        /// </summary>
        public QUnitDefinition(IEnumerable<IQUnitReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;

            this.fileProcessors = fileProcessors;
            fileDependencies["1"] = new[]
                {
                    "chutzpah_boot.js",
                    @"QUnit\v1\qunit.css",
                    @"QUnit\v1\qunit.js"
                };

            fileDependencies["2"] = new[]
                {
                    "chutzpah_boot.js",
                    @"QUnit\v2\qunit.css",
                    @"QUnit\v2\qunit.js",
                };

            testHarness["1"] = @"qunit\v1\qunit.html";
            testHarness["2"] = @"qunit\v2\qunit.html";
            
        }

        public override IEnumerable<string> GetFileDependencies(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return fileDependencies[GetVersion(chutzpahTestSettings)];
        }

        public override string GetTestHarness(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return testHarness[GetVersion(chutzpahTestSettings)];
        }

        /// <summary>
        /// Gets a short, file system friendly key for the QUnit library.
        /// </summary>
        public override string FrameworkKey
        {
            get
            {
                return "qunit";
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable JavaScript QUnit file.
        /// </summary>
        protected override Regex FrameworkSignatureJavaScript
        {
            get
            {
                return RegexPatterns.QUnitTestRegexJavaScript;
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable CoffeeScript QUnit file.
        /// </summary>
        protected override Regex FrameworkSignatureCoffeeScript
        {
            get
            {
                return RegexPatterns.QUnitTestRegexCoffeeScript;
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

        private string GetVersion(ChutzpahTestSettingsFile testSettingsFile)
        {
            // For now default version 1 until the next major release of Chutzpah
            if (string.IsNullOrEmpty(testSettingsFile.FrameworkVersion)
                || (testSettingsFile.FrameworkVersion == "1" || testSettingsFile.FrameworkVersion.StartsWith("1.")))
            {
                return "1";
            }

            return "2";
        }
    }
}
