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
        private List<string> ignorePatterns { get; set; }

        readonly IUrlBuilder urlBuilder;

        public BlanketJsCoverageEngine(IJsonSerializer jsonSerializer, IFileSystemWrapper fileSystem, ILineCoverageMapper lineCoverageMapper, IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
            this.jsonSerializer = jsonSerializer;
            this.fileSystem = fileSystem;
            this.lineCoverageMapper = lineCoverageMapper;

            includePatterns = new List<string>();
            excludePatterns = new List<string>();
            ignorePatterns = new List<string>();
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
            var filesToExcludeFromCoverage = new List<string>();
            var filesToIncludeInCoverage = new List<string>();
            var extensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (TestHarnessItem refScript in harness.ReferencedScripts.Where(rs => rs.HasFile))
            {
                // Skip files which the user is asking us to exclude
                if (!IsFileEligibleForInstrumentation(refScript.ReferencedFile.Path))
                {
                    filesToExcludeFromCoverage.Add(refScript.Attributes["src"]);
                }
                else
                {
                    refScript.Attributes["type"] = "text/blanket"; // prevent Phantom/browser parsing
                }


                // Build extension map for when we conver to regex the include/exclude patterns
                if (!string.IsNullOrEmpty(refScript.ReferencedFile.GeneratedFilePath))
                {
                    var sourceExtension = Path.GetExtension(refScript.ReferencedFile.Path);
                    extensionMap[sourceExtension] = ".js";
                }
            }

            // Construct array of scripts to exclude from instrumentation/coverage collection.
            filesToExcludeFromCoverage.AddRange(
                 harness.TestFrameworkDependencies.Concat(harness.CodeCoverageDependencies)
                 .Where(dep => dep.HasFile && IsScriptFile(dep.ReferencedFile))
                 .Select(dep => dep.Attributes["src"])
                 .Concat(excludePatterns.Select(f => ToRegex(f, extensionMap))));

            filesToIncludeInCoverage.AddRange(includePatterns.Select(f => ToRegex(f, extensionMap)));

            // Name the coverage object so that the JS runner can pick it up.
            harness.ReferencedScripts.Add(new Script(string.Format("window.{0}='_$blanket';", Constants.ChutzpahCoverageObjectReference)));

            // Configure Blanket.
            TestHarnessItem blanketMain = harness.CodeCoverageDependencies.Single(
                                            d => d.Attributes.ContainsKey("src") && (d.Attributes["src"].EndsWith(blanketScriptName) || d.Attributes["src"].Contains(blanketScriptName + "?")));


            string dataCoverNever = "[" + string.Join(",", filesToExcludeFromCoverage.Select(file => "'" + file + "'")) + "]";

            string dataCoverOnly = filesToIncludeInCoverage.Any()
                                   ? "[" + string.Join(",", filesToIncludeInCoverage.Select(file => "'" + file + "'")) + "]"
                                   : "//.*/";

            ChutzpahTracer.TraceInformation("Adding data-cover-never attribute to blanket: {0}", dataCoverNever);

            blanketMain.Attributes.Add("data-cover-flags", "ignoreError autoStart");
            blanketMain.Attributes.Add("data-cover-only", dataCoverOnly);
            blanketMain.Attributes.Add("data-cover-never", dataCoverNever);
            blanketMain.Attributes.Add("data-cover-timeout", testSettingsFile.CodeCoverageTimeout.HasValue ? testSettingsFile.CodeCoverageTimeout.Value.ToString() : "5000");
        }

        /// <summary>
        /// Chutzpah uses glob formats ( http://en.wikipedia.org/wiki/Glob_(programming) ) for its paths.
        /// Blanketjs expects regexes, so we need to convert them
        /// </summary>
        /// <param name="globPath">A filepath in the glob format</param>
        /// <returns>Regular expression</returns>
        private string ToRegex(string globPath, IDictionary<string, string> extensionMap)
        {
            // 0) If that path ends with an extension replace with the result of the extension map
            // 1) Change all backslashes to forward slashes first
            // 2) Escape . (by \\) (NOTE: we are skipping this since blanket has a bug with us using \.
            // 3) Replace * with the regex part ".*" (multiple characters)
            // 4) Replace ? with the regex part "." (single character)
            // 5) Replace [!] with the regex part "[^]" (negative character class)
            // 6) Surround the regex with // and /, and add the modifier i (case insensitive)
            var extension = Path.GetExtension(globPath);
            string mappedExtension;
            if (!string.IsNullOrEmpty(extension) && extensionMap.TryGetValue(extension, out mappedExtension))
            {
                globPath = globPath.Substring(0, globPath.Length - extension.Length) + mappedExtension;
            }

            return string.Format("//{0}/i", globPath.Replace("\\", "/").Replace("*", ".*").Replace("[!", "[^"));
        }

        private bool IsScriptFile(ReferencedFile file)
        {
            string name = file.GeneratedFilePath ?? file.Path;
            return name.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        public CoverageData DeserializeCoverageObject(string json, TestContext testContext)
        {
            var isRunningInWebServer = testContext.TestFileSettings.Server != null && testContext.TestFileSettings.Server.Enabled.GetValueOrDefault();
            var data = jsonSerializer.Deserialize<BlanketCoverageObject>(json);
            ILookup<string, ReferencedFile> generatedToReferencedFile = null;

            if (isRunningInWebServer)
            {
                generatedToReferencedFile = testContext.ReferencedFiles.Where(rf => rf.AbsoluteServerUrl != null).ToLookup(rf => rf.AbsoluteServerUrl, rf => rf);
            }
            else
            {
                generatedToReferencedFile = testContext.ReferencedFiles.Where(rf => rf.GeneratedFilePath != null).ToLookup(rf => rf.GeneratedFilePath, rf => rf);
            }

            var coverageData = new CoverageData(testContext.TestFileSettings.CodeCoverageSuccessPercentage.Value);
            string executedFilePath = null;
            string localFilePathForServerItem = null;

            // Rewrite all keys in the coverage object dictionary in order to change URIs
            // to paths and generated paths to original paths, then only keep the ones
            // that match the include/exclude patterns.
            foreach (var entry in data)
            {
                var referencedFiles = new List<ReferencedFile>();
                Uri uri = new Uri(entry.Key, UriKind.RelativeOrAbsolute);

                if (isRunningInWebServer)
                {
                    if (!uri.IsAbsoluteUri)
                    {
                        string basePath = Path.GetDirectoryName(testContext.TestHarnessPath);
                        var relativePathFromHarness = Path.Combine(basePath, entry.Key);
                        uri = new Uri(urlBuilder.GenerateServerFileUrl(testContext, relativePathFromHarness, true, false, null));
                        localFilePathForServerItem = new Uri(relativePathFromHarness).LocalPath;
                    }

                    executedFilePath = uri.AbsoluteUri;
                }
                else
                {
                    if (!uri.IsAbsoluteUri)
                    {
                        // Resolve against the test file path.
                        string basePath = Path.GetDirectoryName(testContext.TestHarnessPath);
                        uri = new Uri(Path.Combine(basePath, entry.Key));
                    }

                    executedFilePath = uri.LocalPath;

                    // Fix local paths of the form: file:///c:/zzz should become c:/zzz not /c:/zzz
                    // but keep network paths of the form: file://network/files/zzz as //network/files/zzz
                    executedFilePath = RegexPatterns.InvalidPrefixedLocalFilePath.Replace(executedFilePath, "$1");

                    //REMOVE URI Query part from filepath like ?ver=1233123
                    executedFilePath = RegexPatterns.IgnoreQueryPartFromUri.Replace(executedFilePath, "$1");

                    var fileUri = new Uri(executedFilePath, UriKind.RelativeOrAbsolute);
                    executedFilePath = fileUri.LocalPath;
                }

                var matchedFile = generatedToReferencedFile.FirstOrDefault(group => group.Key.IndexOf(executedFilePath, StringComparison.OrdinalIgnoreCase) >= 0);
                if (matchedFile == null)
                {
                    // This does not appear to be a compiled file so just created a referencedFile with the path
                    // In the case of web server mode we use a local file path we generated
                    referencedFiles.Add(new ReferencedFile { Path = localFilePathForServerItem ?? executedFilePath });
                }
                else
                {
                    referencedFiles = matchedFile.ToList();
                }

                referencedFiles = referencedFiles.Where(file => IsFileEligibleForInstrumentation(file.Path)
                                                                && !IsIgnored(file.Path)).ToList();

                foreach (var referencedFile in referencedFiles)
                {
                    // The coveredPath is the file which we have coverage lines for. We assume generated if it exsits otherwise the file path
                    var coveredPath = referencedFile.GeneratedFilePath ?? referencedFile.Path;

                    // If the user is using source maps then always take sourcePath and not generated. 
                    if (testContext.TestFileSettings.Compile != null && testContext.TestFileSettings.Compile.UseSourceMaps.GetValueOrDefault() && referencedFile.SourceMapFilePath != null)
                    {
                        coveredPath = referencedFile.Path;
                    }

                    if (fileSystem.FileExists(coveredPath))
                    {
                        string[] sourceLines = fileSystem.GetLines(coveredPath);
                        int?[] lineExecutionCounts = entry.Value;

                        if (testContext.TestFileSettings.Compile != null && testContext.TestFileSettings.Compile.UseSourceMaps.GetValueOrDefault() && referencedFile.SourceMapFilePath != null)
                        {
                            lineExecutionCounts = this.lineCoverageMapper.GetOriginalFileLineExecutionCounts(entry.Value, sourceLines.Length, referencedFile);
                        }


                        var coverageFileData = new CoverageFileData
                        {
                            LineExecutionCounts = lineExecutionCounts,
                            FilePath = referencedFile.Path,
                            SourceLines = sourceLines
                        };

                        // If some AMD modules has different "non canonical" references (like "../../module" and "./../../module"). Coverage trying to add files many times
                        if (coverageData.ContainsKey(referencedFile.Path))
                        {
                            coverageData[referencedFile.Path].Merge(coverageFileData);
                        }
                        else
                        {
                            coverageData.Add(referencedFile.Path, coverageFileData);
                        }

                    }
                }


            }
            return coverageData;
        }

        public void ClearPatterns()
        {
            includePatterns.Clear();
            excludePatterns.Clear();
            ignorePatterns.Clear();
        }

        public void AddIncludePatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                includePatterns.Add(PrependWildCard(pattern));
            }
        }

        public void AddExcludePatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                excludePatterns.Add(PrependWildCard(pattern));
            }
        }

        public void AddIgnorePatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                ignorePatterns.Add(PrependWildCard(pattern));
            }
        }

        /// <summary>
        /// Prepends the wildcard (*) to the path if it is missing. This helps
        /// since most people don't intend to match the exact begining of a path since that is the
        /// drive root
        /// </summary>
        private string PrependWildCard(string pattern)
        {
            pattern = pattern.Trim();
            if(pattern.Length > 1 && pattern[0] != '*')
            {
                pattern = "*" + pattern;
            }

            return pattern;
        }

        private bool IsFileEligibleForInstrumentation(string filePath)
        {
            // If no include patterns are given then include all files. Otherwise include only the ones that match an include pattern
            if (includePatterns.Any() && !includePatterns.Any(includePattern => NativeImports.PathMatchSpec(filePath, UrlBuilder.NormalizeFilePath(includePattern))))
            {
                return false;
            }

            // If no exclude pattern is given then exclude none otherwise exclude the patterns that match any given exclude pattern
            if (excludePatterns.Any() && excludePatterns.Any(excludePattern => NativeImports.PathMatchSpec(filePath, UrlBuilder.NormalizeFilePath(excludePattern))))
            {
                return false;
            }

            return true;
        }

        public bool IsIgnored(string filePath)
        {
            // If no ignore pattern is given then include all files. Otherwise ignore the ones that match an ignore pattern
            if (ignorePatterns.Any() && ignorePatterns.Any(ignorePattern => NativeImports.PathMatchSpec(filePath, UrlBuilder.NormalizeFilePath(ignorePattern))))
            {
                return true;
            }

            return false;
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
