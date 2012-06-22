namespace Chutzpah.FrameworkDefinitions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Chutzpah.FileProcessors;
    using HtmlAgilityPack;

    /// <summary>
    /// Definition that describes the Jasmine framework.
    /// </summary>
    public class JasmineDefinition : BaseFrameworkDefinition
    {
        private IEnumerable<IJasmineReferencedFileProcessor> fileProcessors;
        private IEnumerable<string> fileDependencies;

        /// <summary>
        /// Initializes a new instance of the JasmineDefinition class.
        /// </summary>
        public JasmineDefinition(IEnumerable<IJasmineReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;
            this.fileDependencies = base.FileDependencies.Concat(new []
                {
                    "jasmine\\jasmine-html.js", 
                    "jasmine\\jasmine_favicon.png"
                });
        }

        /// <summary>
        /// Gets a list of file dependencies to bundle with the Jasmine test harness.
        /// </summary>
        public override IEnumerable<string> FileDependencies
        {
            get
            {
                return this.fileDependencies;
            }
        }

        /// <summary>
        /// Gets a short, file system friendly key for the Jasmine library.
        /// </summary>
        protected override string FrameworkKey
        {
            get
            {
                return "jasmine";
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable Jasmine file.
        /// </summary>
        protected override Regex FrameworkSignature
        {
            get
            {
                return RegexPatterns.JasmineTestRegex;
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

        /// <summary>
        /// Returns the node which will contain test fixture content.
        /// </summary>
        /// <param name="fixtureDocument">The document that contains the node.</param>
        /// <returns>The parent node of text fixture content.</returns>
        protected override HtmlNode GetFixtureNode(HtmlDocument fixtureDocument)
        {
            return fixtureDocument.DocumentNode.SelectSingleNode("/html/body");
        }
    }
}
