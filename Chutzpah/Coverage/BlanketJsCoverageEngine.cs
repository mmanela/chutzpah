using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.Coverage
{
    /// <summary>
    /// Coverage engine that uses Blanket.JS (http://migrii.github.com/blanket/) to do instrumentation
    /// and coverage collection.
    /// </summary>
    public class BlanketJsCoverageEngine : ICoverageEngine
    {
        private readonly IJsonSerializer jsonSerializer;

        public BlanketJsCoverageEngine(IJsonSerializer jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer;
        }

        public IEnumerable<string> GetFileDependencies(IFrameworkDefinition definition)
        {
            FrameworkSpecificInfo info = GetInfo(definition);
            yield return "Coverage\\" + info.BlanketScriptName;
        }

        public string IncludePattern { get; set; }
        
        public string ExcludePattern { get; set; }

        public void PrepareTestHarnessForCoverage(TestHarness harness, IFrameworkDefinition definition)
        {
            FrameworkSpecificInfo info = GetInfo(definition);
            bool foundFilesToCover = false;
            foreach (HtmlTag tag in harness.ReferencedScripts)
            {
                string originalFilePath = tag.ReferencedFile.Path;
                if (IsFileEligibleForInstrumentation(originalFilePath))
                {
                    tag.Attributes.Add("data-cover", "");
                    tag.Attributes["type"] = "text/blanket"; // prevent Phantom/browser parsing
                    foundFilesToCover = true;
                }
            }
            if (foundFilesToCover)
            {
                // Name the coverage object so that the JS runner can pick it up.
                harness.TestFrameworkDependencies.Add(new Script(string.Format("window.{0}='_$blanket';", Constants.ChutzpahCoverageObjectReference)));

                // Tell Blanket to ignore parse errors.
                HtmlTag blanketMain =
                    harness.TestFrameworkDependencies.Single(
                        d => d.Attributes.ContainsKey("src") && d.Attributes["src"].EndsWith(info.BlanketScriptName));
                blanketMain.Attributes.Add("data-cover-ignore-error", "");
            }
        }

        public CoverageData DeserializeCoverageObject(string json, TestContext testContext)
        {
            BlanketCoverageObject data = jsonSerializer.Deserialize<BlanketCoverageObject>(json);
            IDictionary<string, string> generatedToOriginalFilePath =
                testContext.ReferencedJavaScriptFiles.Where(rf => rf.GeneratedFilePath != null).ToDictionary(rf => rf.GeneratedFilePath, rf => rf.Path);
            
            CoverageData coverageData = new CoverageData();

            // Rewrite all keys in the coverage object dictionary in order to change URIs
            // to paths and generated paths to original paths.
            foreach (var entry in data)
            {
                string filePath = new Uri(entry.Key).LocalPath;
                string newKey;
                if (!generatedToOriginalFilePath.TryGetValue(filePath, out newKey))
                {
                    newKey = filePath;
                }
                coverageData.Add(newKey, new CoverageFileData
                                             {
                                                 LineExecutionCounts = entry.Value,
                                                 FilePath = newKey
                                             });
            }
            return coverageData;
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);

        private bool IsFileEligibleForInstrumentation(string filePath)
        {
            if (IncludePattern != null && !PathMatchSpec(filePath, IncludePattern)) return false;
            if (ExcludePattern != null && PathMatchSpec(filePath, ExcludePattern)) return false;
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

        private static IDictionary<Type, FrameworkSpecificInfo> FrameworkInfoMap =
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
