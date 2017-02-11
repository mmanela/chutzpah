using Chutzpah.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Chutzpah.FileProcessors;

namespace Chutzpah.FrameworkDefinitions
{

    /// <summary>
    /// Definition that describes the Jasmine framework.
    /// </summary>
    public class JasmineDefinition : BaseFrameworkDefinition
    {
        private IEnumerable<IJasmineReferencedFileProcessor> fileProcessors;
        private IDictionary<string, IEnumerable<string>> fileDependencies = new Dictionary<string, IEnumerable<string>>();
        private IDictionary<string, string> testHarness = new Dictionary<string, string>();
        private IDictionary<string, string> testRunner = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the JasmineDefinition class.
        /// </summary>
        public JasmineDefinition(IEnumerable<IJasmineReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;
            fileDependencies["1"] = new[]
                {
                    "chutzpah_boot.js",
                    @"jasmine\v1\jasmine.css", 
                    @"jasmine\v1\jasmine.js", 
                    @"jasmine\v1\jasmine-html.js", 
                    @"jasmine\v1\jasmine_favicon.png",
                    @"jasmine\v1\jasmine-ddescribe-iit.js"
                };

            fileDependencies["2"] = new[]
                {
                    "chutzpah_boot.js",
                    @"jasmine\v2\jasmine.css", 
                    @"jasmine\v2\jasmine.js", 
                    @"jasmine\v2\jasmine-html.js", 
                    @"jasmine\v2\boot.js", 
                    @"jasmine\v2\jasmine_favicon.png"
                };

            testHarness["1"] = @"jasmine\v1\jasmine.html";
            testHarness["2"] = @"jasmine\v2\jasmine.html";

            testRunner["1"] = @"ChutzpahJSRunners\jasmineRunnerV1.js";
            testRunner["2"] = @"ChutzpahJSRunners\jasmineRunnerV2.js";
        }

        /// <summary>
        /// Gets a list of file dependencies to bundle with the Jasmine test harness.
        /// </summary>
        /// <param name="chutzpahTestSettings"></param>
        public override IEnumerable<string> GetFileDependencies(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return fileDependencies[GetVersion(chutzpahTestSettings)];
        }

        public override string GetTestHarness(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return testHarness[GetVersion(chutzpahTestSettings)];
        }

        public override string GetTestRunner(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            return testRunner[GetVersion(chutzpahTestSettings)];
        }

        public override string GetBlanketScriptName(ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            var version = GetVersion(chutzpahTestSettings);
            return "blanket_jasmine_v" + version + ".js";
        }

        /// <summary>
        /// Gets a short, file system friendly key for the Jasmine library.
        /// </summary>
        public override string FrameworkKey
        {
            get
            {
                return "jasmine";
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable Jasmine file in a JavaScript file.
        /// </summary>
        protected override Regex FrameworkSignatureJavaScript
        {
            get
            {
                return RegexPatterns.JasmineTestRegexJavaScript;
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable Jasmine file in a CoffeeScript file.
        /// </summary>
        protected override Regex FrameworkSignatureCoffeeScript
        {
            get
            {
                return RegexPatterns.JasmineTestRegexCoffeeScript;
            }
        }

        /// <summary>
        /// Gets a list of file processors to call within the Process method.
        /// </summary>
        protected override IEnumerable<IReferencedFileProcessor> FileProcessors
        {
            get
            {
                return this.fileProcessors;
            }
        }

        private string GetVersion(ChutzpahTestSettingsFile testSettingsFile)
        {
            if (!string.IsNullOrEmpty(testSettingsFile.FrameworkVersion) 
                && (testSettingsFile.FrameworkVersion == "1" || testSettingsFile.FrameworkVersion.StartsWith("1.")))
            {
                return "1";
            }

            return "2";
        }
    }
}
