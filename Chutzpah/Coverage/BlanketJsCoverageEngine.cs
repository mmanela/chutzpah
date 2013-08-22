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

        public BlanketJsCoverageEngine(IJsonSerializer jsonSerializer, IFileSystemWrapper fileSystem)
        {
            this.jsonSerializer = jsonSerializer;
            this.fileSystem = fileSystem;

            IncludePatterns = new List<string>();
            ExcludePatterns = new List<string>();
        }

        public IEnumerable<string> GetFileDependencies(IFrameworkDefinition definition)
        {
            FrameworkSpecificInfo info = GetInfo(definition);
            yield return "Coverage\\" + info.BlanketScriptName;
        }

        public ICollection<string> IncludePatterns { get; set; }

        public ICollection<string> ExcludePatterns { get; set; }

        public void PrepareTestHarnessForCoverage(TestHarness harness, IFrameworkDefinition definition)
        {
            FrameworkSpecificInfo info = GetInfo(definition);

            // Construct array of scripts to exclude from instrumentation/coverage collection.
            IList<string> filesToExcludeFromCoverage =
                harness.TestFrameworkDependencies
                .Where(dep => dep.HasFile && IsScriptFile(dep.ReferencedFile))
                .Select(dep => dep.Attributes["src"])
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

            // We run some code after blanket it loaded to modify behavior a bit.
            // First we name the coverage object so that the JS runner can pick it up.
            // Second, we forward the load function which blanket overrides onto the real require function. 
            // This is needed since Chutzpah overrides require and define so when blanket tried to override the load method it doesn't get the real one.
            const string blanketConfigureScript = @"
    	    window.{0}='_$blanket';
            if(window.chutzpah && window.requirejs){{
    	        window.chutzpah.cachedRequire.load = window.requirejs.load;
            }}";

            harness.ReferencedScripts.Add(new Script(string.Format(blanketConfigureScript, Constants.ChutzpahCoverageObjectReference)));

            // Configure Blanket.
            TestHarnessItem blanketMain = harness.TestFrameworkDependencies.Single(
                                            d => d.Attributes.ContainsKey("src") && d.Attributes["src"].EndsWith(info.BlanketScriptName));

            string dataCoverNever = "[" + string.Join(",", filesToExcludeFromCoverage.Select(file => "'" + file + "'")) + "]";

            blanketMain.Attributes.Add("data-cover-flags", "ignoreError autoStart");
            blanketMain.Attributes.Add("data-cover-only", "//.*/");
            blanketMain.Attributes.Add("data-cover-never", dataCoverNever);
        }

        private bool IsScriptFile(ReferencedFile file)
        {
            string name = file.GeneratedFilePath ?? file.Path;
            return name.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        public CoverageData DeserializeCoverageObject(string json, TestContext testContext)
        {
            BlanketCoverageObject data = jsonSerializer.Deserialize<BlanketCoverageObject>(json);
            IDictionary<string, string> generatedToOriginalFilePath =
                testContext.ReferencedJavaScriptFiles.Where(rf => rf.GeneratedFilePath != null).ToDictionary(rf => rf.GeneratedFilePath, rf => rf.Path);

            CoverageData coverageData = new CoverageData();

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

                string newKey;
                if (!generatedToOriginalFilePath.TryGetValue(filePath, out newKey))
                {
                    newKey = filePath;
                }

                if (IsFileEligibleForInstrumentation(newKey))
                {
                    string[] sourceLines = fileSystem.GetLines(filePath);
                    coverageData.Add(newKey, new CoverageFileData
                                                 {
                                                     LineExecutionCounts = entry.Value,
                                                     FilePath = newKey,
                                                     SourceLines = sourceLines
                                                 });
                }
            }
            return coverageData;
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);

        private bool IsFileEligibleForInstrumentation(string filePath)
        {
            // If no include patterns are given then include all files. Otherwise include only the ones that match an include pattern
            if (IncludePatterns.Any() && !IncludePatterns.Any(includePattern => PathMatchSpec(filePath, includePattern)))
            {
                return false;
            }

            // If no exclude pattern is given then exclude none otherwise exclude the patterns that match any given exclude pattern
            if (ExcludePatterns.Any() && ExcludePatterns.Any(excludePattern => PathMatchSpec(filePath, excludePattern)))
            {
                return false;
            }

            return true;
        }

        private class BlanketCoverageObject : Dictionary<string, int?[]>
        {
        }

        private FrameworkSpecificInfo GetInfo(IFrameworkDefinition def)
        {
            FrameworkSpecificInfo info;
            if (!FrameworkInfoMap.TryGetValue(def.GetType(), out info))
            {
                throw new ArgumentException("Unknown framework: " + def.GetType().Name);
            }
            return info;
        }

        private static readonly IDictionary<Type, FrameworkSpecificInfo> FrameworkInfoMap =
            new Dictionary<Type, FrameworkSpecificInfo>
                {
                    {
                        typeof (JasmineDefinition), new FrameworkSpecificInfo
                                                        {
                                                            BlanketScriptName = "blanket_jasmine.js"
                                                        }
                        },
                    {
                        typeof (QUnitDefinition), new FrameworkSpecificInfo
                                                      {
                                                          BlanketScriptName = "blanket_qunit.js"
                                                      }
                        }
                };

        private class FrameworkSpecificInfo
        {
            internal string BlanketScriptName { get; set; }
        }
    }
}
