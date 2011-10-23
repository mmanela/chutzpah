namespace Chutzpah.Frameworks
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface that describes a test framework.
    /// </summary>
    public interface IFrameworkDefinition
    {
        /// <summary>
        /// Gets a list of file dependencies to bundle with the framework test harness.
        /// </summary>
        IEnumerable<string> FileDependencies { get; }

        /// <summary>
        /// Gets the file name of the HTML test harness to use with the framework.
        /// </summary>
        string TestHarness { get; }

        /// <summary>
        /// Gets the file name of the JavaScript test runner to use with the framework.
        /// </summary>
        string TestRunner { get; }

        /// <summary>
        /// Tests whether the given file contents uses the framework.
        /// </summary>
        /// <param name="fileContents">Contents of the file as a string to test.</param>
        /// <param name="bestGuess">True if the method should fall back to best guess detection.</param>
        /// <returns>True if the file uses the framework, otherwise false.</returns>
        bool FileUsesFramework(string fileContents, bool bestGuess);

        /// <summary>
        /// Tests whether the given file is the framework itself or one of its core dependencies.
        /// </summary>
        /// <param name="referenceFileName">File name of a reference to test.</param>
        /// <returns>True if the file is a framework dependency, otherwise false.</returns>
        bool ReferenceIsDependency(string referenceFileName);

        /// <summary>
        /// Returns the fixture content within a custom test harness.
        /// </summary>
        /// <param name="harnessText">The contents of a test harness.</param>
        /// <returns>The fixture content from a test harness if it exists.</returns>
        string GetFixtureContent(string harnessText);
    }
}
