namespace Chutzpah.FrameworkDefinitions
{
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;

    /// <summary>
    /// Definition that describes the QUnit framework.
    /// </summary>
    public class QUnitDefinition : BaseFrameworkDefinition
    {
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

        public override IReferencedFileProcessor LineNumberProcessor
        {
            get { throw new System.NotImplementedException(); }
        }
    }
}
