using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.FileProcessors;
using Chutzpah.Models;

namespace Chutzpah.FrameworkDefinitions
{
    /// <summary>
    /// Abstract definition that provides a convention based implementation of IFrameworkDefinition.
    /// </summary>
    public abstract class BaseFrameworkDefinition : IFrameworkDefinition
    {
        private static readonly Regex FrameworkReferenceRegex =
            new Regex(@"\<(?:script|reference|chutzpah_reference).*?(?:src|path)\s*=\s*[""'].*?(?<framework>(qunit|jasmine|mocha)).*?\.js[""']",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual string GetBlanketScriptName(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return "blanket_" + FrameworkKey + ".js";
        }

        /// <summary>
        /// Gets a short, file system friendly key for the framework library.
        /// </summary>
        public abstract string FrameworkKey { get; }

        /// <summary>
        /// Gets a regular expression pattern to match a testable javascript file.
        /// </summary>
        protected virtual Regex FrameworkSignatureJavaScript { get { return null; } }

        /// <summary>
        /// Gets a regular expression pattern to match a testable coffeescript file.
        /// </summary>
        protected virtual Regex FrameworkSignatureCoffeeScript { get { return null; } }

        /// <summary>
        /// Gets a list of file processors to call within the Process method.
        /// </summary>
        protected abstract IEnumerable<IReferencedFileProcessor> FileProcessors { get; }

        /// <summary>
        /// Gets a list of file dependencies to bundle with the framework test harness.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        public abstract IEnumerable<string> GetFileDependencies(ChutzpahTestSettingsFile chutzpahTestSettings);

        /// <summary>
        /// Gets the file name of the test harness to use with the framework.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        public abstract string GetTestHarness(ChutzpahTestSettingsFile chutzpahTestSettings);

        /// <summary>
        /// Gets the file name of the JavaScript test runner to use with the framework.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        public virtual string GetTestRunner(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return @"ChutzpahJSRunners\" + FrameworkKey + "Runner.js";
        }

        /// <summary>
        /// Tests whether the given file contents uses the framework.
        /// </summary>
        /// <param name="fileContents">Contents of the file as a string to test.</param>
        /// <param name="bestGuess">True if the method should fall back from definitive to best guess detection.</param>
        /// <param name="pathType">The type of the file being tests</param>
        /// <returns>True if the file is a framework dependency, otherwise false.</returns>
        public virtual bool FileUsesFramework(string fileContents, bool bestGuess, PathType pathType)
        {
            if (bestGuess)
            {
                var regex = FrameworkSignatureJavaScript;
                return regex != null && regex.IsMatch(fileContents);
            }

            Match match = FrameworkReferenceRegex.Match(fileContents);
            return match.Success &&
                   match.Groups["framework"].Value.Equals(FrameworkKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests whether the given file is the framework itself or one of its core dependencies.
        /// </summary>
        /// <param name="referenceFileName">File name of a reference to test.</param>
        /// <param name="testSettingsFile"></param>
        /// <returns>True if the file is a framework dependency, otherwise false.</returns>
        public virtual bool ReferenceIsDependency(string referenceFileName, ChutzpahTestSettingsFile testSettingsFile)
        {
            string fileName = Path.GetFileName(referenceFileName);
            if (!string.IsNullOrEmpty(fileName))
            {
                return GetFileDependencies(testSettingsFile).Any(x => fileName.Equals(Path.GetFileName(x), StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Processes a referenced file according to the framework's needs.
        /// </summary>
        /// <param name="referencedFile">A referenced file to process.</param>
        /// <param name="testFileText"></param>
        /// <param name="settings"></param>
        public void Process(ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings)
        {
            if (FileProcessors != null)
            {
                foreach (IReferencedFileProcessor item in FileProcessors)
                {
                    item.Process(this, referencedFile, testFileText, settings);
                }
            }
        }

        public virtual Dictionary<string, string> GetFrameworkReplacements(ChutzpahTestSettingsFile chutzpahTestSettings, string testFilePath, string testFileText)
        {
            return new Dictionary<string, string>();
        }
    }
}