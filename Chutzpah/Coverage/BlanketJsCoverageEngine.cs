using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using SourceMapDotNet;

namespace Chutzpah.Coverage
{
    /// <summary>
    /// Coverage engine that uses Blanket.JS (http://blanketjs.org/) to do instrumentation
    /// and coverage collection.
    /// </summary>
    public class BlanketJsCoverageEngine : ICoverageEngine
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly IJsonSerializer jsonSerializer;
        private readonly ILineCoverageMapper lineCoverageMapper;

        private List<string> includePatterns { get; set; }
        private List<string> excludePatterns { get; set; }

        public BlanketJsCoverageEngine(IJsonSerializer jsonSerializer, IFileSystemWrapper fileSystem, ILineCoverageMapper lineCoverageMapper)
        {
            this.jsonSerializer = jsonSerializer;
            this.fileSystem = fileSystem;
            this.lineCoverageMapper = lineCoverageMapper;

            includePatterns = new List<string>();
            excludePatterns = new List<string>();
        }

        public IEnumerable<string> GetFileDependencies(IFrameworkDefinition definition, ChutzpahTestSettingsFile testSettingsFile)
        {
            var blanketScriptName = GetBlanketScriptName(definition, testSettingsFile);
            yield return "Coverage\\" + blanketScriptName;
        }


        public void PrepareTestHarnessForCoverage(TestHarness harness, IFrameworkDefinition definition, ChutzpahTestSettingsFile testSettingsFile)
        {
            string blanketScriptName = GetBlanketScriptName(definition, testSettingsFile);

            // Construct array of scripts to exclude from instrumentation/coverage collection.
            IList<string> filesToExcludeFromCoverage =
                harness.TestFrameworkDependencies.Concat(harness.CodeCoverageDependencies)
                .Where(dep => dep.HasFile && IsScriptFile(dep.ReferencedFile))
                .Select(dep => dep.Attributes["src"])
                .Concat(excludePatterns.Select(ToRegex))
                .ToList();

            foreach (TestHarnessItem refScript in harness.ReferencedScripts.Where(rs => rs.HasFile))
            {
                // Exclude files which the user is asking us to ignores
                if (!IsFileEligibleForInstrumentation(refScript.ReferencedFile.Path))
                {
                    filesToExcludeFromCoverage.Add(refScript.Attributes["src"]);
                }
                else
                {
                    refScript.Attributes["type"] = "text/blanket"; // prevent Phantom/browser parsing
                }
            }

            // Name the coverage object so that the JS runner can pick it up.
            harness.ReferencedScripts.Add(new Script(string.Format("window.{0}='_$blanket';", Constants.ChutzpahCoverageObjectReference)));

            // Configure Blanket.
            TestHarnessItem blanketMain = harness.CodeCoverageDependencies.Single(
                                            d => d.Attributes.ContainsKey("src") && d.Attributes["src"].EndsWith(blanketScriptName));

            string dataCoverNever = "[" + string.Join(",", filesToExcludeFromCoverage.Select(file => "'" + file + "'")) + "]";


            ChutzpahTracer.TraceInformation("Adding data-cover-never attribute to blanket: {0}", dataCoverNever);

            blanketMain.Attributes.Add("data-cover-flags", "ignoreError autoStart");
            blanketMain.Attributes.Add("data-cover-only", "//.*/");
            blanketMain.Attributes.Add("data-cover-never", dataCoverNever);
        }

        /// <summary>
        /// Chutzpah uses glob formats ( http://en.wikipedia.org/wiki/Glob_(programming) ) for its paths.
        /// Blanketjs expects regexes, so we need to convert them
        /// </summary>
        /// <param name="globPath">A filepath in the glob format</param>
        /// <returns>Regular expression</returns>
        private string ToRegex(string globPath)
        {
            // 1) Change all backslashes to forward slashes first
            // 2) Escape . (by \\)
            // 3) Replace * with the regex part ".*" (multiple characters)
            // 4) Replace ? with the regex part "." (single character)
            // 5) Replace [!] with the regex part "[^]" (negative character class)
            // 6) Surround the regex with // and /, and add the modifier i (case insensitive)
            return string.Format("//{0}/i", globPath.Replace("\\", "\\/").Replace(".", "\\.").Replace("*", ".*").Replace("*", ".").Replace("[!", "[^"));
        }

        private bool IsScriptFile(ReferencedFile file)
        {
            string name = file.GeneratedFilePath ?? file.Path;
            return name.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        public CoverageData DeserializeCoverageObject(string json, TestContext testContext)
        {
            var data = jsonSerializer.Deserialize<BlanketCoverageObject>(json);
            IDictionary<string, ReferencedFile> generatedToReferencedFile =
                testContext.ReferencedFiles.Where(rf => rf.GeneratedFilePath != null).ToDictionary(rf => rf.GeneratedFilePath, rf => rf);

            var coverageData = new CoverageData(testContext.TestFileSettings.CodeCoverageSuccessPercentage);

            // Rewrite all keys in the coverage object dictionary in order to change URIs
            // to paths and generated paths to original paths, then only keep the ones
            // that match the include/exclude patterns.
            foreach (var entry in data)
            {
                Uri uri = new Uri(entry.Key, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                {
                    // Resolve against the test file path.
                    string basePath = Path.GetDirectoryName(testContext.TestHarnessPath);
                    uri = new Uri(Path.Combine(basePath, entry.Key));
                }

                string filePath = uri.LocalPath;

                // Fix local paths of the form: file:///c:/zzz should become c:/zzz not /c:/zzz
                // but keep network paths of the form: file://network/files/zzz as //network/files/zzz
                filePath = RegexPatterns.InvalidPrefixedLocalFilePath.Replace(filePath, "$1");
                var fileUri = new Uri(filePath, UriKind.RelativeOrAbsolute);
                filePath = fileUri.LocalPath;

                ReferencedFile referencedFile;
                string newKey;
                if (!generatedToReferencedFile.TryGetValue(filePath, out referencedFile))
                {
                    newKey = filePath;
                }
                else
                {
                    newKey = referencedFile.Path;
                    if (referencedFile.SourceMapFilePath != null && testContext.TestFileSettings.UseSourceMaps)
                    {
                        filePath = referencedFile.Path;
                    }
                }

                if (IsFileEligibleForInstrumentation(newKey))
                {
                    string[] sourceLines = fileSystem.GetLines(filePath);
                    int?[] lineExecutionCounts = entry.Value;

                    if (testContext.TestFileSettings.UseSourceMaps && referencedFile.SourceMapFilePath != null)
                    {
                        lineExecutionCounts = this.lineCoverageMapper.GetOriginalFileLineExecutionCounts(entry.Value, sourceLines.Length, referencedFile.SourceMapFilePath);
                    }

                    coverageData.Add(newKey, new CoverageFileData
                    {
                        LineExecutionCounts = lineExecutionCounts,
                        FilePath = newKey,
                        SourceLines = sourceLines
                    });
                }
            }
            return coverageData;
        }

        public void ClearPatterns()
        {
            includePatterns.Clear();
            excludePatterns.Clear();
        }

        public void AddIncludePatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                includePatterns.Add(pattern);
            }
        }

        public void AddExcludePatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                excludePatterns.Add(pattern);
            }
        }

        private bool IsFileEligibleForInstrumentation(string filePath)
        {
            // If no include patterns are given then include all files. Otherwise include only the ones that match an include pattern
            if (includePatterns.Any() && !includePatterns.Any(includePattern => NativeImports.PathMatchSpec(filePath, FileProbe.NormalizeFilePath(includePattern))))
            {
                return false;
            }

            // If no exclude pattern is given then exclude none otherwise exclude the patterns that match any given exclude pattern
            if (excludePatterns.Any() && excludePatterns.Any(excludePattern => NativeImports.PathMatchSpec(filePath, FileProbe.NormalizeFilePath(excludePattern))))
            {
                return false;
            }

            return true;
        }

        private string GetBlanketScriptName(IFrameworkDefinition def, ChutzpahTestSettingsFile settingsFile)
        {
            return def.GetBlanketScriptName(settingsFile);
        }

        public class BlanketCoverageObject : Dictionary<string, int?[]>
        {

        }
    }
}
