namespace Chutzpah.FrameworkDefinitions
{
    using System.Text.RegularExpressions;
    using Chutzpah.FileProcessors;
    using HtmlAgilityPack;

    /// <summary>
    /// Definition that describes the QUnit framework.
    /// </summary>
    public class QUnitDefinition : BaseFrameworkDefinition
    {
        private IReferencedFileProcessor lineNumberProcessor;

        /// <summary>
        /// Constructs a new QUnitDefinition.
        /// </summary>
        public QUnitDefinition()
        {
            this.lineNumberProcessor = ChutzpahContainer.Current.GetInstance<QUnitLineNumberProcessor>();
        }

        /// <summary>
        /// Gets a processor to assign line numbers to tests.
        /// </summary>
        public override IReferencedFileProcessor LineNumberProcessor
        {
            get
            {
                return this.lineNumberProcessor;
            }
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
        /// Gets a regular expression pattern to match a testable QUnit file.
        /// </summary>
        protected override Regex FrameworkSignature
        {
            get
            {
                return RegexPatterns.QUnitTestRegex;
            }
        }

        /// <summary>
        /// Returns the node which will contain test fixture content.
        /// </summary>
        /// <param name="fixtureDocument">The document that contains the node.</param>
        /// <returns>The parent node of text fixture content.</returns>
        protected override HtmlNode GetFixtureNode(HtmlDocument fixtureDocument)
        {
            return fixtureDocument.GetElementbyId(this.FrameworkKey + "-fixture");
        }
    }
}
