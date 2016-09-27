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
            ExpandReferenceComments = true;
            Includes = new List<string>();
            Excludes = new List<string>();
            TemplateOptions = new TemplateOptions();
        }

        public ReferencePathSettings(SettingsFileReference settingsFileReference)
        {
            ExpandReferenceComments = settingsFileReference.ExpandReferenceComments;

            Includes = settingsFileReference.Includes;
            Excludes = settingsFileReference.Excludes;
            IncludeInTestHarness = settingsFileReference.IncludeInTestHarness;
            IsTestFrameworkFile = settingsFileReference.IsTestFrameworkFile;
            TemplateOptions = settingsFileReference.TemplateOptions;
        }

        /// <summary>
        /// This determines if the reference path processing should read the file contents
        /// to find more references. This is set to false when the reference comes from chutzpah.json file since at that point
        /// the user is able to specify whatever they want
        /// </summary>
        public bool ExpandReferenceComments { get; set; }

        public ICollection<string> Includes { get; set; }
        public ICollection<string> Excludes { get; set; }
        public bool IncludeInTestHarness { get; set; }
        public bool IsTestFrameworkFile { get; set; }
        public TemplateOptions TemplateOptions { get; set; }
    }

    public interface IReferenceProcessor
    {
        /// <summary>
        /// Scans the test file extracting all referenced files from it.
        /// </summary>
        /// <param name="referencedFiles">The list of referenced files</param>
        /// <param name="definition">Test framework defintition</param>
        /// <param name="chutzpahTestSettings"></param>
        /// <returns></returns>
        void GetReferencedFiles(List<ReferencedFile> referencedFiles, IFrameworkDefinition definition, ChutzpahTestSettingsFile chutzpahTestSettings);

        void SetupAmdFilePaths(List<ReferencedFile> referencedFiles, string testHarnessDirectory, ChutzpahTestSettingsFile testSettings);

        void SetupPathsFormattedForTestHarness(TestContext testContext, List<ReferencedFile> referencedFiles);
    }

    public class ReferenceProcessor : IReferenceProcessor
    {
        readonly IFileSystemWrapper fileSystem;
        readonly IFileProbe fileProbe;
        readonly IUrlBuilder urlBuilder;

        public ReferenceProcessor(IFileSystemWrapper fileSystem, IFileProbe fileProbe, IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
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
        /// <param name="chutzpahTestSettings"></param>
        /// <returns></returns>
        public void GetReferencedFiles(List<ReferencedFile> referencedFiles, IFrameworkDefinition definition, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            var filesUnderTests = referencedFiles.Where(x => x.IsFileUnderTest).ToList();

            var referencePathSet = new HashSet<string>(referencedFiles.Select(x => x.Path), StringComparer.OrdinalIgnoreCase);

            // Process the references that the user specifies in the chutzpah settings file
            foreach (var reference in chutzpahTestSettings.References.Where(reference => reference != null))
            {
                // The path we assume default to the chuzpah.json directory if the Path property is not set
                var referencePath = string.IsNullOrEmpty(reference.Path) ? reference.SettingsFileDirectory : reference.Path;

                ProcessFilePathAsReference(
                    referencePathSet,
                    definition,
                    reference.SettingsFileDirectory,
                    chutzpahTestSettings,
                    referencePath,
                    referencedFiles,
                    new ReferencePathSettings(reference));
            }

            // Process the references defined using /// <reference comments in test file contents
            foreach (var fileUnderTest in filesUnderTests)
            {
                var testFileText = fileSystem.GetText(fileUnderTest.Path);

                definition.Process(fileUnderTest, testFileText, chutzpahTestSettings);

                if (fileUnderTest.ExpandReferenceComments)
                {
                    var result = GetReferencedFiles(
                        referencePathSet,
                        definition,
                        testFileText,
                        fileUnderTest.Path,
                        chutzpahTestSettings);


                    var flattenedReferenceTree = from root in result
                                                 from flattened in FlattenReferenceGraph(root)
                                                 select flattened;

                    referencedFiles.AddRange(flattenedReferenceTree);
                }
            }
        }


        /// <summary>
        /// Adds the paths for when running in a web server or local file system
        /// </summary>
        public void SetupPathsFormattedForTestHarness(TestContext testContext, List<ReferencedFile> referencedFiles)
        {
            foreach (var referencedFile in referencedFiles)
            {
                var referencePath = referencedFile.GeneratedFilePath ?? referencedFile.Path;
                referencedFile.AbsoluteServerUrl = urlBuilder.GenerateAbsoluteServerUrl(testContext, referencedFile);
                referencedFile.PathForUseInTestHarness = urlBuilder.GenerateFileUrl(testContext, referencedFile);
            }

        }



        /// <summary>
        /// Add the AMD file paths for the Path and GeneratePath fields
        /// </summary>
        public void SetupAmdFilePaths(List<ReferencedFile> referencedFiles, string testHarnessDirectory, ChutzpahTestSettingsFile testSettings)
        {
            // If the legacy BasePath setting it set then defer to that
            if (!string.IsNullOrEmpty(testSettings.AMDBasePath))
            {
                SetupLegacyAmdFilePaths(referencedFiles, testHarnessDirectory, testSettings);
                return;
            }

            // If AMDAppDirectory is set make amd paths relative to that
            // Else if AMDBaseUrl is set make the amd path relative to that
            // Otherwise make amd paths relative to test harness directory and make AMDBaseUrl the test harness directory
            testSettings.AMDBaseUrl = string.IsNullOrEmpty(testSettings.AMDBaseUrl) ? testHarnessDirectory : testSettings.AMDBaseUrl;
            string appRoot = string.IsNullOrEmpty(testSettings.AMDAppDirectory) ? testSettings.AMDBaseUrl : testSettings.AMDAppDirectory;

            foreach (var referencedFile in referencedFiles)
            {
                referencedFile.AmdFilePath = GetAmdPath(referencedFile.Path, appRoot);
            }
        }

        private static string GetAmdPath(string filePath, string amdAppRoot)
        {
            string amdModulePath = UrlBuilder.GetRelativePath(amdAppRoot, filePath);

            amdModulePath = NormalizeAmdModulePath(amdModulePath);

            return amdModulePath;
        }

        private static string NormalizeAmdModulePath(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))
                .Replace("\\", "/")
                .Trim('/', '\\');
        }

        /// <summary>
        /// Add the AMD file paths for the Path and GeneratePath fields
        /// </summary>
        /// <param name="referencedFiles"></param>
        /// <param name="testHarnessDirectory"></param>
        /// <param name="testSettings"></param>
        private void SetupLegacyAmdFilePaths(List<ReferencedFile> referencedFiles, string testHarnessDirectory, ChutzpahTestSettingsFile testSettings)
        {
            // If the user set a AMD base path then we must relativize the amd path's using the path from the base path to the test harness directory
            string relativeAmdRootPath = "";
            if (!string.IsNullOrEmpty(testSettings.AMDBasePath))
            {
                relativeAmdRootPath = UrlBuilder.GetRelativePath(testSettings.AMDBasePath, testHarnessDirectory);
            }

            foreach (var referencedFile in referencedFiles)
            {
                referencedFile.AmdFilePath = GetLegacyAmdPath(testHarnessDirectory, referencedFile.Path, relativeAmdRootPath);

                if (!string.IsNullOrEmpty(referencedFile.GeneratedFilePath))
                {
                    referencedFile.AmdGeneratedFilePath = GetLegacyAmdPath(testHarnessDirectory, referencedFile.GeneratedFilePath, relativeAmdRootPath);
                }
            }
        }

        private static string GetLegacyAmdPath(string testHarnessDirectory, string filePath, string relativeAmdRootPath)
        {
            string amdModulePath = UrlBuilder.GetRelativePath(testHarnessDirectory, filePath);

            amdModulePath = NormalizeAmdModulePath(Path.Combine(relativeAmdRootPath, amdModulePath));

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
                string referencePath = null, templateId = null, templateType = null;
                TemplateMode templateMode = TemplateMode.Raw;

                for (var i = 0; i < match.Groups["PropName"].Captures.Count; i++)
                {
                    var propName = match.Groups["PropName"].Captures[i].Value.ToLowerInvariant();
                    var propValue = match.Groups["PropValue"].Captures[i].Value;

                    switch (propName)
                    {
                        case "path":
                            referencePath = propValue;
                            break;
                        case "id":
                            templateId = propValue;
                            break;
                        case "type":
                            templateType = propValue;
                            break;
                        case "mode":
                            if (propValue.Equals("script", StringComparison.OrdinalIgnoreCase))
                            {
                                templateMode = TemplateMode.Script;
                            }
                            break;
                        default:
                            break;
                    }
                }

                referencePath = AdjustPathIfRooted(chutzpahTestSettings, referencePath);
                string relativeReferencePath = Path.Combine(Path.GetDirectoryName(currentFilePath), referencePath);
                string absoluteFilePath = fileProbe.FindFilePath(relativeReferencePath);
                if (referencedFiles.All(r => r.Path != absoluteFilePath))
                {
                    ChutzpahTracer.TraceInformation("Added html template '{0}' to referenced files", absoluteFilePath);
                    referencedFiles.Add(new ReferencedFile
                    {
                        Path = absoluteFilePath,
                        IsLocal = false,
                        IncludeInTestHarness = true,
                        TemplateOptions = new TemplateOptions
                        {
                            Mode = templateMode,
                            Id = templateId,
                            Type = templateType
                        }
                    });
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
            if (definition.ReferenceIsDependency(referenceFileName, chutzpahTestSettings))
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
                    var includePatterns = pathSettings.Includes.Select(x => UrlBuilder.NormalizeFilePath(x)).ToList();
                    var excludePatterns = pathSettings.Excludes.Select(x => UrlBuilder.NormalizeFilePath(x)).ToList();

                    // Find all files in this folder including sub-folders. This can be ALOT of files.
                    // Only a subset of these files Chutzpah might understand so many of these will be ignored.
                    var childFiles = fileSystem.GetFiles(absoluteFolderPath, "*.*", SearchOption.AllDirectories);
                    var validFiles = from file in childFiles
                                     let normalizedFile = UrlBuilder.NormalizeFilePath(file)
                                     where !fileProbe.IsTemporaryChutzpahFile(file)
                                     && (!includePatterns.Any() || includePatterns.Any(pat => NativeImports.PathMatchSpec(normalizedFile, pat)))
                                     && (!excludePatterns.Any() || !excludePatterns.Any(pat => NativeImports.PathMatchSpec(normalizedFile, pat)))
                                     select file;

                    validFiles.ForEach(file => VisitReferencedFile(file, definition, discoveredPaths, referencedFiles, chutzpahTestSettings, pathSettings));

                    return;
                }

                // At this point we know that this file/folder does not exist!
                ChutzpahTracer.TraceWarning("Referenced file '{0}' which was resolved to '{1}' does not exist", referencePath, relativeReferencePath);
            }
            else if (referenceUri.IsAbsoluteUri)
            {
                var referencedFile = new ReferencedFile
                {
                    Path = referencePath,
                    IsLocal = false,
                    IncludeInTestHarness = true,
                    IsTestFrameworkFile = pathSettings.IsTestFrameworkFile,
                    TemplateOptions = pathSettings.TemplateOptions
                };

                ChutzpahTracer.TraceInformation(
                    "Added file '{0}' to referenced files. Local: {1}, IncludeInTestHarness: {2}",
                    referencedFile.Path,
                    referencedFile.IsLocal,
                    referencedFile.IncludeInTestHarness);
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
            if (discoveredPaths.Contains(absoluteFilePath)) return null;

            var referencedFile = new ReferencedFile
            {
                Path = absoluteFilePath,
                IsLocal = true,
                IsTestFrameworkFile = pathSettings.IsTestFrameworkFile,
                IncludeInTestHarness = pathSettings.IncludeInTestHarness || chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.Normal,
                TemplateOptions = pathSettings.TemplateOptions
            };

            ChutzpahTracer.TraceInformation(
                "Added file '{0}' to referenced files. Local: {1}, IncludeInTestHarness: {2}",
                referencedFile.Path,
                referencedFile.IsLocal,
                referencedFile.IncludeInTestHarness);

            referencedFiles.Add(referencedFile);
            discoveredPaths.Add(referencedFile.Path); // Remmember this path to detect reference loops


            ChutzpahTracer.TraceInformation("Processing referenced file '{0}' for expanded references", absoluteFilePath);
            if (pathSettings.ExpandReferenceComments)
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