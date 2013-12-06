using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.Extensions;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class ReferencePathSettings
    {
        public ReferencePathSettings()
        {
            ExpandNestedReferences = true;
        }

        public ReferencePathSettings(SettingsFileReference settingsFileReference)
        {
            ExpandNestedReferences = false;

            Include = settingsFileReference.Include;
            Exclude = settingsFileReference.Exclude;
            IncludeInTestHarness = settingsFileReference.IncludeInTestHarness;
            IsTestFrameworkFile = settingsFileReference.IsTestFrameworkFile;
        }

        /// <summary>
        /// This determines if the reference path processing should read the file contents
        /// to find more references. This is set to false when the reference comes from chutzpah.json file since at that point
        /// the user is able to specify whatever they want
        /// </summary>
        public bool ExpandNestedReferences { get; set; }

        public string Include { get; set; }
        public string Exclude { get; set; }
        public bool IncludeInTestHarness { get; set; }
        public bool IsTestFrameworkFile { get; set; }
    }

    public interface IReferenceProcessor
    {
        /// <summary>
        /// Scans the test file extracting all referenced files from it.
        /// </summary>
        /// <param name="referencedFiles">The list of referenced files</param>
        /// <param name="definition">Test framework defintition</param>
        /// <param name="textToParse">The content of the file to parse and extract from</param>
        /// <param name="currentFilePath">Path to the file under test</param>
        /// <returns></returns>
        void GetReferencedFiles(
            List<ReferencedFile> referencedFiles,
            IFrameworkDefinition definition,
            string textToParse,
            string currentFilePath,
            ChutzpahTestSettingsFile chutzpahTestSettings);

        void SetupAmdFilePaths(List<ReferencedFile> referencedFiles, string testHarnessDirectory);
    }

    public class ReferenceProcessor : IReferenceProcessor
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly IFileProbe fileProbe;

        public ReferenceProcessor(IFileSystemWrapper fileSystem, IFileProbe fileProbe)
        {
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
        }

        /// <summary>
        /// Scans the test file extracting all referenced files from it.
        /// </summary>
        /// <param name="referencedFiles">The list of referenced files</param>
        /// <param name="definition">Test framework defintition</param>
        /// <param name="textToParse">The content of the file to parse and extract from</param>
        /// <param name="currentFilePath">Path to the file under test</param>
        /// <returns></returns>
        public void GetReferencedFiles(
            List<ReferencedFile> referencedFiles,
            IFrameworkDefinition definition,
            string textToParse,
            string currentFilePath,
            ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            var referencePathSet = new HashSet<string>(referencedFiles.Select(x => x.Path));

            // Process the references that the user specifies in the chutzpah settings file
            foreach (var reference in chutzpahTestSettings.References.Where(reference => reference != null))
            {
                ProcessFilePathAsReference(
                    referencePathSet,
                    definition,
                    chutzpahTestSettings.SettingsFileDirectory,
                    chutzpahTestSettings,
                    reference.Path,
                    referencedFiles,
                    new ReferencePathSettings(reference));
            }

            // Process the references defined using /// <reference comments in test file contents
            IList<ReferencedFile> result = GetReferencedFiles(
                referencePathSet,
                definition,
                textToParse,
                currentFilePath,
                chutzpahTestSettings);


            IEnumerable<ReferencedFile> flattenedReferenceTree = from root in result
                from flattened in FlattenReferenceGraph(root)
                select flattened;
            referencedFiles.AddRange(flattenedReferenceTree);
        }

        /// <summary>
        /// Add the AMD file paths for the Path and GeneratePath fields
        /// </summary>
        /// <param name="referencedFiles"></param>
        /// <param name="testHarnessDirectory"></param>
        public void SetupAmdFilePaths(List<ReferencedFile> referencedFiles, string testHarnessDirectory)
        {
            foreach (var referencedFile in referencedFiles)
            {
                referencedFile.AmdFilePath = GetAmdPath(testHarnessDirectory, referencedFile.Path);

                if (!string.IsNullOrEmpty(referencedFile.GeneratedFilePath))
                {
                    referencedFile.AmdGeneratedFilePath = GetAmdPath(testHarnessDirectory, referencedFile.GeneratedFilePath);
                }

            }
        }

        private static string GetAmdPath(string testHarnessDirectory, string filePath)
        {
            string amdModulePath = "";
            if (filePath.Contains(testHarnessDirectory))
            {
                amdModulePath = filePath
                    .Replace(Path.GetExtension(filePath), "")
                    .Replace(testHarnessDirectory, "")
                    .Replace("\\", "/")
                    .Trim('/', '\\');
            }
            return amdModulePath;
        }

        private IList<ReferencedFile> GetReferencedFiles(
            HashSet<string> discoveredPaths,
            IFrameworkDefinition definition,
            string textToParse,
            string currentFilePath,
            ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            var referencedFiles = new List<ReferencedFile>();

            Regex regex = RegexPatterns.JsReferencePathRegex;
            foreach (Match match in regex.Matches(textToParse))
            {
                if (!ShouldIncludeReference(match)) continue;

                string referencePath = match.Groups["Path"].Value;

                ProcessFilePathAsReference(
                    discoveredPaths,
                    definition,
                    currentFilePath,
                    chutzpahTestSettings,
                    referencePath,
                    referencedFiles,
                    new ReferencePathSettings());
            }

            foreach (Match match in RegexPatterns.JsTemplatePathRegex.Matches(textToParse))
            {
                string referencePath = match.Groups["Path"].Value;

                referencePath = AdjustPathIfRooted(chutzpahTestSettings, referencePath);
                string relativeReferencePath = Path.Combine(Path.GetDirectoryName(currentFilePath), referencePath);
                string absoluteFilePath = fileProbe.FindFilePath(relativeReferencePath);
                if (referencedFiles.All(r => r.Path != absoluteFilePath))
                {
                    ChutzpahTracer.TraceInformation("Added html template '{0}' to referenced files", absoluteFilePath);
                    referencedFiles.Add(new ReferencedFile { Path = absoluteFilePath, IsLocal = false, IncludeInTestHarness = true });
                }
            }

            return referencedFiles;
        }

        private void ProcessFilePathAsReference(
            HashSet<string> discoveredPaths,
            IFrameworkDefinition definition,
            string relativeProcessingPath,
            ChutzpahTestSettingsFile chutzpahTestSettings,
            string referencePath,
            List<ReferencedFile> referencedFiles,
            ReferencePathSettings pathSettings)
        {
            ChutzpahTracer.TraceInformation("Investigating reference file path '{0}'", referencePath);

            // Check test settings and adjust the path if it is rooted (e.g. /some/path)
            referencePath = AdjustPathIfRooted(chutzpahTestSettings, referencePath);

            var referenceUri = new Uri(referencePath, UriKind.RelativeOrAbsolute);
            string referenceFileName = Path.GetFileName(referencePath);

            //  Ignore test runner, since we use our own.
            if (definition.ReferenceIsDependency(referenceFileName))
            {
                ChutzpahTracer.TraceInformation(
                    "Ignoring reference file '{0}' as a duplicate reference to {1}",
                    referenceFileName,
                    definition.FrameworkKey);
                return;
            }

            var isRelativeUri = !referenceUri.IsAbsoluteUri;

            // If this either a relative uri or a file uri 
            if (isRelativeUri || referenceUri.IsFile)
            {
                var relativeProcessingPathFolder = fileSystem.FolderExists(relativeProcessingPath)
                    ? relativeProcessingPath
                    : Path.GetDirectoryName(relativeProcessingPath);
                string relativeReferencePath = Path.Combine(relativeProcessingPathFolder, referencePath);

                // Check if reference is a file
                string absoluteFilePath = fileProbe.FindFilePath(relativeReferencePath);
                if (absoluteFilePath != null)
                {
                    VisitReferencedFile(absoluteFilePath, definition, discoveredPaths, referencedFiles, chutzpahTestSettings, pathSettings);
                    return;
                }
                
                // Check if reference is a folder
                string absoluteFolderPath = fileProbe.FindFolderPath(relativeReferencePath);
                if (absoluteFolderPath != null)
                {
                    // Find all files in this folder including sub-folders. This can be ALOT of files.
                    // Only a subset of these files Chutzpah might understand so many of these will be ignored.
                    var childFiles = fileSystem.GetFiles(absoluteFolderPath, "*.*", SearchOption.AllDirectories);
                    var validFiles = from file in childFiles
                        where !fileProbe.IsTemporaryChutzpahFile(file)
                                && (pathSettings.Include == null || NativeImports.PathMatchSpec(file, pathSettings.Include))
                                && (pathSettings.Exclude == null || !NativeImports.PathMatchSpec(file, pathSettings.Exclude))
                        select file;

                    validFiles
                        .ForEach(file => VisitReferencedFile(file, definition, discoveredPaths, referencedFiles, chutzpahTestSettings, pathSettings));

                    return;
                }

                // At this point we know that this file/folder does not exist!
                ChutzpahTracer.TraceWarning("Referenced file '{0}' which was resolved to '{1}' does not exist", referencePath, relativeProcessingPathFolder);
                
            }
            else if (referenceUri.IsAbsoluteUri)
            {
                var referencedFile = new ReferencedFile
                {
                    Path = referencePath,
                    IsLocal = false,
                    IncludeInTestHarness = true,
                    IsTestFrameworkFile = pathSettings.IsTestFrameworkFile,
                };

                ChutzpahTracer.TraceInformation("Added file '{0}' to referenced files. Local: {1}, IncludeInTestHarness: {2}", referencedFile.Path, referencedFile.IsLocal, referencedFile.IncludeInTestHarness);
                referencedFiles.Add(referencedFile);
            }
        }

        /// <summary>
        /// If the reference path is rooted (e.g. /some/path) and the user chose to adjust it then change it
        /// </summary>
        /// <returns></returns>
        private static string AdjustPathIfRooted(ChutzpahTestSettingsFile chutzpahTestSettings, string referencePath)
        {
            if (chutzpahTestSettings.RootReferencePathMode == RootReferencePathMode.SettingsFileDirectory &&
                (referencePath.StartsWith("/") || referencePath.StartsWith("\\")))
            {
                ChutzpahTracer.TraceInformation(
                    "Changing reference '{0}' to be rooted from settings directory '{1}'",
                    referencePath,
                    chutzpahTestSettings.SettingsFileDirectory);

                referencePath = chutzpahTestSettings.SettingsFileDirectory + referencePath;
            }

            return referencePath;
        }

        private ReferencedFile VisitReferencedFile(
            string absoluteFilePath,
            IFrameworkDefinition definition,
            HashSet<string> discoveredPaths,
            ICollection<ReferencedFile> referencedFiles,
            ChutzpahTestSettingsFile chutzpahTestSettings,
            ReferencePathSettings pathSettings)
        {
            // If the file doesn't exit exist or we have seen it already then return
            if (discoveredPaths.Any(x => x.Equals(absoluteFilePath, StringComparison.OrdinalIgnoreCase))) return null;

            var referencedFile = new ReferencedFile
            {
                Path = absoluteFilePath,
                IsLocal = true,
                IsTestFrameworkFile = pathSettings.IsTestFrameworkFile,
                IncludeInTestHarness = pathSettings.IncludeInTestHarness || chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.Normal
            };

            ChutzpahTracer.TraceInformation("Added file '{0}' to referenced files. Local: {1}, IncludeInTestHarness: {2}", referencedFile.Path, referencedFile.IsLocal, referencedFile.IncludeInTestHarness);

            referencedFiles.Add(referencedFile);
            discoveredPaths.Add(referencedFile.Path); // Remmember this path to detect reference loops


            ChutzpahTracer.TraceInformation("Processing referenced file '{0}' for expanded references", absoluteFilePath);
            if (pathSettings.ExpandNestedReferences)
            {
                referencedFile.ReferencedFiles = ExpandNestedReferences(discoveredPaths, definition, absoluteFilePath, chutzpahTestSettings);
            }

            return referencedFile;
        }

        private IList<ReferencedFile> ExpandNestedReferences(
            HashSet<string> discoveredPaths,
            IFrameworkDefinition definition,
            string currentFilePath,
            ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            try
            {
                string textToParse = fileSystem.GetText(currentFilePath);
                return GetReferencedFiles(discoveredPaths, definition, textToParse, currentFilePath, chutzpahTestSettings);
            }
            catch (IOException e)
            {
                // Unable to get file text
                ChutzpahTracer.TraceError(e, "Unable to get file text from test reference with path {0}", currentFilePath);
            }

            return new List<ReferencedFile>();
        }

        private static IEnumerable<ReferencedFile> FlattenReferenceGraph(ReferencedFile rootFile)
        {
            var flattenedFileList = new List<ReferencedFile>();
            foreach (ReferencedFile childFile in rootFile.ReferencedFiles)
            {
                flattenedFileList.AddRange(FlattenReferenceGraph(childFile));
            }
            flattenedFileList.Add(rootFile);

            return flattenedFileList;
        }

        /// <summary>
        /// Decides whether a reference match should be included.
        /// </summary>
        /// <param name="match">The reference match.</param>
        /// <returns>
        /// <c>true</c> if the reference should be included, otherwise <c>false</c>.
        /// </returns>
        private static bool ShouldIncludeReference(Match match)
        {
            if (match.Success)
            {
                var exclude = match.Groups["Exclude"].Value;

                if (string.IsNullOrWhiteSpace(exclude)
                    || exclude.Equals("false", StringComparison.OrdinalIgnoreCase)
                    || exclude.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    // The exclude flag is empty or negative
                    return true;
                }
            }

            ChutzpahTracer.TraceInformation("Excluding reference file because it contains a postitive chutzpah-exclude attribute");

            return false;
        }
    }
}