using System;
using System.Data.SqlTypes;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah.FrameworkDefinitions
{

    /// <summary>
    /// Interface that describes a test framework.
    /// </summary>
    public interface IFrameworkDefinition
    {
        /// <summary>
        /// Gets a list of file dependencies to bundle with the framework test harness.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        IEnumerable<string> GetFileDependencies(ChutzpahTestSettingsFile chutzpahTestSettings);

        /// <summary>
        /// Gets the file name of the HTML test harness to use with the framework.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        string GetTestHarness(ChutzpahTestSettingsFile chutzpahTestSettings);

        /// <summary>
        /// Gets the file name of the JavaScript test runner to use with the framework.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        /// <param name="options"></param>
        string GetTestRunner(ChutzpahTestSettingsFile chutzpahTestSettings, TestOptions options);

        /// <summary>
        /// Gets the name of the blanket script to use for code coverage
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        /// <returns></returns>
        string GetBlanketScriptName(ChutzpahTestSettingsFile chutzpahTestSettings);

        /// <summary>
        /// Gets a short, file system friendly key for the framework library.
        /// </summary>
        string FrameworkKey { get; }

        /// <summary>
        /// Tests whether the given file contents uses the framework.
        /// </summary>
        /// <param name="fileContents">Contents of the file as a string to test.</param>
        /// <param name="bestGuess">True if the method should fall back to best guess detection.</param>
        /// <param name="pathType">The type of the file being tests</param>
        /// <returns>True if the file uses the framework, otherwise false.</returns>
        bool FileUsesFramework(string fileContents, bool bestGuess, PathType pathType);

        /// <summary>
        /// Tests whether the given file is the framework itself or one of its core dependencies.
        /// </summary>
        /// <param name="referenceFileName">File name of a reference to test.</param>
        /// <param name="testSettingsFile"></param>
        /// <returns>True if the file is a framework dependency, otherwise false.</returns>
        bool ReferenceIsDependency(string referenceFileName, ChutzpahTestSettingsFile testSettingsFile);

        /// <summary>
        /// Processes a referenced file according to the framework's needs.
        /// </summary>
        /// <param name="referencedFile">A referenced file to process.</param>
        /// <param name="testFileText"></param>
        /// <param name="settings"></param>
        void Process(ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings);

        Dictionary<string, string> GetFrameworkReplacements(ChutzpahTestSettingsFile chutzpahTestSettings, string testFilePath, string testFileText);
    }
}
