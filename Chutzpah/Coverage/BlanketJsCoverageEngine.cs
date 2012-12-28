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
            yield return string.Format("Coverage\\blanket_{0}.js", definition.FrameworkKey);
        }

        public string IncludePattern { get; set; }
        
        public string ExcludePattern { get; set; }

        public void PrepareTestHarnessForCoverage(TestHarness harness, IFrameworkDefinition definition)
        {
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
                harness.ReferencedScripts.Add(new Script(string.Format("window.{0}='_$blanket'", Constants.ChutzpahCoverageObjectReference)));

                // Auto-run coverage for QUnit.
                if (definition.FrameworkKey.Equals("qunit"))
                {
                    harness.ReferencedScripts.Add(new Script("QUnit.urlParams.coverage=true"));
                }
                
                // Add a reference to the main Blanket script.
                string coverageFile = string.Format("blanket_{0}.js", definition.FrameworkKey);
                ReferencedFile coverageRefFile = new ReferencedFile {Path = coverageFile};
                harness.ReferencedScripts.Add(new Script(coverageRefFile));
            }
        }

        public CoverageData DeserializeCoverageObject(string json, TestContext testContext)
        {
            CoverageData data = jsonSerializer.Deserialize<CoverageData>(json);
            IDictionary<string, string> generatedToOriginalFilePath =
                testContext.ReferencedJavaScriptFiles.Where(rf => rf.GeneratedFilePath != null).ToDictionary(rf => rf.GeneratedFilePath, rf => rf.Path);

            // Rewrite all keys in the coverage object dictionary to change URIs
            // to paths and generated paths to original paths.
            foreach (string scriptPath in data.Keys.ToList()) // copy to avoid concurrent modification
            {
                string filePath = new Uri(scriptPath).LocalPath;
                string newKey;
                if (!generatedToOriginalFilePath.TryGetValue(filePath, out newKey))
                {
                    newKey = filePath;
                }
                data[newKey] = data[scriptPath];
                data.Remove(scriptPath);
            }
            return data;
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);

        private bool IsFileEligibleForInstrumentation(string filePath)
        {
            if (IncludePattern != null && !PathMatchSpec(filePath, IncludePattern)) return false;
            if (ExcludePattern != null && PathMatchSpec(filePath, ExcludePattern)) return false;
            return true;
        }
    }
}
