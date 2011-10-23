namespace Chutzpah.Frameworks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;

    /// <summary>
    /// Abstract definition that provides a convention based implementation of IFrameworkDefinition.
    /// </summary>
    public abstract class BaseFrameworkDefinition : IFrameworkDefinition
    {
        /// <summary>
        /// Gets a list of file dependencies to bundle with the framework test harness.
        /// </summary>
        public virtual IEnumerable<string> FileDependencies
        {
            get
            {
                return new string[] { this.FrameworkKey + ".js", this.FrameworkKey + ".css" };
            }
        }

        /// <summary>
        /// Gets the file name of the test harness to use with the framework.
        /// </summary>
        public virtual string TestHarness
        {
            get
            {
                return this.FrameworkKey + ".html";
            }
        }

        /// <summary>
        /// Gets the file name of the JavaScript test runner to use with the framework.
        /// </summary>
        public virtual string TestRunner
        {
            get
            {
                return this.FrameworkKey + "Runner.js";
            }
        }

        /// <summary>
        /// Gets a short, file system friendly key for the framework library.
        /// </summary>
        protected abstract string FrameworkKey { get; }

        /// <summary>
        /// Gets a regular expression pattern to match a testable file.
        /// </summary>
        protected abstract Regex FrameworkSignature { get; }

        /// <summary>
        /// Tests whether the given file contents uses the framework.
        /// </summary>
        /// <param name="fileContents">Contents of the file as a string to test.</param>
        /// <param name="bestGuess">True if the method should fall back from definitive to best guess detection.</param>
        /// <returns>True if the file is a framework dependency, otherwise false.</returns>
        public virtual bool FileUsesFramework(string fileContents, bool bestGuess)
        {
            if (bestGuess)
            {
                return this.FrameworkSignature.IsMatch(fileContents);
            }

            var referencePattern = new Regex(@"\<(script|reference).*(src|path)\s*=\s*[""'].*" + this.FrameworkKey + @"\.js[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return referencePattern.IsMatch(fileContents);
        }

        /// <summary>
        /// Tests whether the given file is the framework itself or one of its core dependencies.
        /// </summary>
        /// <param name="referenceFileName">File name of a reference to test.</param>
        /// <returns>True if the file is a framework dependency, otherwise false.</returns>
        public virtual bool ReferenceIsDependency(string referenceFileName)
        {
            if (!string.IsNullOrEmpty(referenceFileName))
            {
                return this.FileDependencies.Any(x => x.Equals(referenceFileName, StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Returns the fixture content within a custom test harness.
        /// </summary>
        /// <param name="harnessText">The contents of a test harness.</param>
        /// <returns>The fixture content from a test harness if it exists.</returns>
        public virtual string GetFixtureContent(string harnessText)
        {
            var fixtureContent = string.Empty;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(harnessText);
            var testFixture = this.GetFixtureNode(htmlDocument);

            if (testFixture != null)
            {
                fixtureContent = testFixture.InnerHtml;
            }

            return fixtureContent;
        }

        /// <summary>
        /// Returns the node which will contain test fixture content.
        /// </summary>
        /// <param name="fixtureDocument">The document that contains the node.</param>
        /// <returns>The parent node of text fixture content.</returns>
        protected abstract HtmlNode GetFixtureNode(HtmlDocument fixtureDocument);
    }
}
