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
        private IEnumerable<string> fileDependencies;

        /// <summary>
        /// Initializes a new instance of the QUnitDefinition class.
        /// </summary>
        public QUnitDefinition(IEnumerable<IQUnitReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors; 
            this.fileDependencies = new[]
                {
                    "QUnit\\qunit.css", 
                    "QUnit\\qunit.js"
                };
        }

        public override IEnumerable<string> GetFileDependencies(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return fileDependencies;
        }

        public override string GetTestHarness(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return @"QUnit\qunit.html";
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
    }
}
