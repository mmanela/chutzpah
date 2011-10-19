namespace Chutzpah.FrameworkDefinitions
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

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
        /// <returns>True if the file uses the framework, otherwise false.</returns>
        public virtual bool FileUsesFramework(string fileContents)
        {
            return this.FrameworkSignature.IsMatch(fileContents);
        }

        /// <summary>
        /// Tests whether the given file is the framework itself.
        /// </summary>
        /// <param name="referenceFileName">File name of a reference to test.</param>
        /// <returns>True of the file is the same as the framework, otherwise false.</returns>
        public virtual bool ReferenceIsFramework(string referenceFileName)
        {
            var frameworkPattern = new Regex("^" + this.FrameworkKey + ".js$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return frameworkPattern.IsMatch(referenceFileName);
        }
    }
}
