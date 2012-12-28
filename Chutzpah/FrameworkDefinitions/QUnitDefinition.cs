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

        /// <summary>
        /// Initializes a new instance of the QUnitDefinition class.
        /// </summary>
        public QUnitDefinition(IEnumerable<IQUnitReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;
        }

        /// <summary>
        /// Gets a short, file system friendly key for the QUnit library.
        /// </summary>
        protected override string FrameworkKey
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
