using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class TestHarness
    {
        readonly TestContext testContext;
        readonly ChutzpahTestSettingsFile chutzpahTestSettings;
        readonly TestOptions testOptions;
        readonly IEnumerable<ReferencedFile> referencedFiles;
        readonly IFileSystemWrapper fileSystem;
        readonly IUrlBuilder urlBuilder;

        public IList<TestHarnessItem> CodeCoverageDependencies { get; private set; }
        public IList<TestHarnessItem> TestFrameworkDependencies { get; private set; }
        public IList<TestHarnessItem> ReferencedHtmlTemplates { get; private set; }
        public IList<TestHarnessItem> ReferencedScripts { get; private set; }
        public IList<TestHarnessItem> ReferencedStyles { get; private set; }

        public TestHarness(TestContext testContext, TestOptions testOptions, IEnumerable<ReferencedFile> referencedFiles, IFileSystemWrapper fileSystem, IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
            this.testContext = testContext;
            this.chutzpahTestSettings = testContext.TestFileSettings;
            this.testOptions = testOptions;
            this.referencedFiles = referencedFiles;
            this.fileSystem = fileSystem;

            BuildTags(referencedFiles);
            CleanupTestHarness();
        }

        public string CreateHtmlText(string testHtmlTemplate, Dictionary<string, string> frameworkReplacements)
        {
            var testJsReplacement = new StringBuilder();
            var testFrameworkDependencies = new StringBuilder();
            var codeCoverageDependencies = new StringBuilder();
            var referenceJsReplacement = new StringBuilder();
            var referenceCssReplacement = new StringBuilder();
            var referenceHtmlTemplateReplacement = new StringBuilder();

            BuildReferenceHtml(testFrameworkDependencies,
                               referenceCssReplacement,
                               testJsReplacement,
                               referenceJsReplacement,
                               referenceHtmlTemplateReplacement,
                               codeCoverageDependencies);


            string amdTestFilePathArrayString = "";
            string amdModuleMap = "";
            if (chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.AMD)
            {
                amdTestFilePathArrayString = BuildAmdTestFileArrayString();
                amdModuleMap = BuildModuleMapForGeneratedFiles();
            }

            var amdBasePathUrl = "";

            if (!string.IsNullOrEmpty(chutzpahTestSettings.AMDBasePath))
            {
                amdBasePathUrl = urlBuilder.GenerateFileUrl(testContext, chutzpahTestSettings.AMDBasePath, fullyQualified: true);
            }
            else if (!string.IsNullOrEmpty(chutzpahTestSettings.AMDBaseUrl))
            {
                amdBasePathUrl = urlBuilder.GenerateFileUrl(testContext, chutzpahTestSettings.AMDBaseUrl, fullyQualified: true);
            }

            var replacements = new Dictionary<string, string>
            {
                {"TestFrameworkDependencies", testFrameworkDependencies.ToString()},
                {"CodeCoverageDependencies", codeCoverageDependencies.ToString()},
                {"TestJSFile", testJsReplacement.ToString()},
                {"ReferencedJSFiles", referenceJsReplacement.ToString()},
                {"ReferencedCSSFiles", referenceCssReplacement.ToString()},
                {"TestHtmlTemplateFiles", referenceHtmlTemplateReplacement.ToString()},
                {"AMDTestPath", amdTestFilePathArrayString},
                {"AMDModuleMap", amdModuleMap},
                {"AMDBasePath",  amdBasePathUrl }
            };

            var testHtmlStringBuilder = new StringBuilder(testHtmlTemplate);

            foreach (var replacement in replacements.Union(frameworkReplacements))
            {
                testHtmlStringBuilder.Replace("@@" + replacement.Key + "@@", replacement.Value);
            }

            return testHtmlStringBuilder.ToString();
        }

        /// <summary>
        /// Generates the module map which is used to map from the original amd file path to the generated one
        /// This is only needed in the AMDBasePath legacy setting which tries to generate absolute paths for all files.
        /// This is not desirable anymore since its simpler and more flexible to let a user layout their files and specify baseurl
        /// </summary>
        /// <returns></returns>
        private string BuildModuleMapForGeneratedFiles()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(chutzpahTestSettings.AMDBasePath))
            {
                foreach (var referencedFile in referencedFiles.Where(x => !string.IsNullOrEmpty(x.GeneratedFilePath)))
                {
                    builder.AppendFormat("\"{0}\":\"{1}\",\n", referencedFile.AmdFilePath, referencedFile.AmdGeneratedFilePath);
                }
            }

            return builder.ToString();
        }


        /// <summary>
        /// Build a string representation of the array of test files
        /// </summary>
        /// <returns></returns>
        private string BuildAmdTestFileArrayString()
        {
            var builder = new StringBuilder();
            foreach (var referencedFile in referencedFiles.Where(x => x.IsFileUnderTest))
            {
                builder.AppendFormat("\"{0}\",", referencedFile.AmdFilePath);
            }

            return builder.ToString();
        } 

        private void BuildTags(IEnumerable<ReferencedFile> referencedFilePaths)
        {
            ReferencedHtmlTemplates = new List<TestHarnessItem>();
            ReferencedScripts = new List<TestHarnessItem>();
            ReferencedStyles = new List<TestHarnessItem>();
            TestFrameworkDependencies = new List<TestHarnessItem>();
            CodeCoverageDependencies = new List<TestHarnessItem>();
            var seenPathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ReferencedFile referencedFile in referencedFilePaths)
            {
                string referencePath = string.IsNullOrEmpty(referencedFile.GeneratedFilePath)
                                        ? referencedFile.Path
                                        : referencedFile.GeneratedFilePath;

                // Skip paths we processed already
                if (seenPathSet.Contains(referencePath))
                {
                    continue;
                }
                else
                {
                    seenPathSet.Add(referencePath);
                }
                 
                IList<TestHarnessItem> refList = ChooseRefList(referencedFile, referencePath);
                if (refList == null) continue;

                if (referencePath.EndsWith(Constants.CssExtension, StringComparison.OrdinalIgnoreCase))
                {
                    refList.Add(new ExternalStylesheet(referencedFile));
                }
                else if (referencePath.EndsWith(Constants.PngExtension, StringComparison.OrdinalIgnoreCase))
                {
                    refList.Add(new ShortcutIcon(referencedFile));
                }
                else if (referencePath.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    refList.Add(new Script(referencedFile));
                }
                else if (referencePath.EndsWith(Constants.HtmlScriptExtension, StringComparison.OrdinalIgnoreCase) ||
                         referencePath.EndsWith(Constants.HtmScriptExtension, StringComparison.OrdinalIgnoreCase) ||
                         referencePath.EndsWith(Constants.CSHtmlScriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    refList.Add(new Html(referencedFile, fileSystem));
                }
            }
        }

        private IList<TestHarnessItem> ChooseRefList(ReferencedFile referencedFile, string referencePath)
        {
            var codeCoverageEnabled = testOptions.CoverageOptions.ShouldRunCoverage(chutzpahTestSettings.CodeCoverageExecutionMode);

            // If CodeCoverage is enabled and we are in Execution mode make sure we load requirejs before the code coverage files
            var referencedFileName = Path.GetFileName(referencedFile.Path);
            var amdLoader = codeCoverageEnabled
                            && !string.IsNullOrEmpty(referencedFileName)
                            && RegexPatterns.IsRequireJsFileName.IsMatch(referencedFileName)
                            && testOptions.TestExecutionMode == TestExecutionMode.Execution;

            IList<TestHarnessItem> list = null;
            if (referencedFile.IsTestFrameworkFile)
            {
                list = TestFrameworkDependencies;
            }
            else if (referencedFile.IsCodeCoverageDependency || amdLoader)
            {
                list = CodeCoverageDependencies;
            }
            else if (referencePath.EndsWith(Constants.CssExtension, StringComparison.OrdinalIgnoreCase))
            {
                list = ReferencedStyles;
            }
            else if (referencePath.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase))
            {
                list = ReferencedScripts;
            }
            else if (referencePath.EndsWith(Constants.HtmlScriptExtension, StringComparison.OrdinalIgnoreCase) ||
                     referencePath.EndsWith(Constants.HtmScriptExtension, StringComparison.OrdinalIgnoreCase) ||
                     referencePath.EndsWith(Constants.CSHtmlScriptExtension, StringComparison.OrdinalIgnoreCase))
            {
                list = ReferencedHtmlTemplates;
            }
            return list;
        }

        private void BuildReferenceHtml(StringBuilder testFrameworkDependencies, StringBuilder referenceCssReplacement, StringBuilder testJsReplacement, StringBuilder referenceJsReplacement, StringBuilder referenceHtmlTemplateReplacement, StringBuilder codeCoverageDependencies)
        {
            foreach (TestHarnessItem item in TestFrameworkDependencies)
            {
                testFrameworkDependencies.AppendLine(item.ToString());
            }

            foreach (TestHarnessItem item in CodeCoverageDependencies)
            {
                codeCoverageDependencies.AppendLine(item.ToString());
            }

            foreach (TestHarnessItem item in ReferencedScripts.Where(x => !x.HasFile || x.ReferencedFile.IncludeInTestHarness))
            {

                if (item.ReferencedFile != null && item.ReferencedFile.IsFileUnderTest)
                {
                    testJsReplacement.AppendLine(item.ToString());
                }
                else
                {
                    referenceJsReplacement.AppendLine(item.ToString());
                }
            }

            foreach (TestHarnessItem item in ReferencedStyles)
            {
                referenceCssReplacement.AppendLine(item.ToString());
            }

            foreach (TestHarnessItem item in ReferencedHtmlTemplates)
            {
                referenceHtmlTemplateReplacement.AppendLine(item.ToString());
            }
        }

        private void CleanupTestHarness()
        {
            // TODO: Remove this need for this by updating the logic in the framework definition to support regex matches in ReferenceIsDependency

            // Remove additional references to QUnit.
            // (Iterate over a copy to avoid concurrent modification of the list!)
            foreach (TestHarnessItem reference in ReferencedScripts.Where(r => r.HasFile).ToList())
            {
                if (reference.ReferencedFile.IsFileUnderTest) continue;

                string fileName = Path.GetFileName(reference.ReferencedFile.Path);
                if (!string.IsNullOrEmpty(fileName) && RegexPatterns.IsQUnitFileName.IsMatch(fileName))
                {
                    ReferencedScripts.Remove(reference);
                }
            }
        }
    }

    public class TestHarnessItem
    {
        private readonly bool explicitEndTag;
        private readonly string contents;
        private readonly string tagName;

        public IDictionary<string, string> Attributes { get; private set; }
        public ReferencedFile ReferencedFile { get; private set; }
        public bool HasFile { get { return ReferencedFile != null; } }

        internal TestHarnessItem(ReferencedFile referencedFile, string tagName, bool explicitEndTag)
            : this(tagName, explicitEndTag)
        {
            ReferencedFile = referencedFile;
        }

        internal TestHarnessItem(string contents, string tagName, bool explicitEndTag)
            : this(tagName, explicitEndTag)
        {
            this.contents = contents;
        }

        private TestHarnessItem(string tagName, bool explicitEndTag)
        {
            this.tagName = tagName;
            this.explicitEndTag = explicitEndTag;
            Attributes = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("<");
            builder.Append(tagName);
            foreach (var entry in Attributes)
            {
                if (entry.Value == null)
                {
                    builder.AppendFormat(@" {0} ", entry.Key);
                }
                else
                {
                    builder.AppendFormat(@" {0}=""{1}""", entry.Key, entry.Value);
                }
            }
            if (explicitEndTag || contents != null)
            {
                builder.AppendFormat(">{1}</{0}>", tagName, contents ?? "");
            }
            else
            {
                builder.Append("/>");
            }
            return builder.ToString();
        }

    }

    public class ExternalStylesheet : TestHarnessItem
    {
        public ExternalStylesheet(ReferencedFile referencedFile)
            : base(referencedFile, "link", false)
        {
            Attributes.Add("rel", "stylesheet");
            Attributes.Add("type", "text/css");
            Attributes.Add("href", referencedFile.PathForUseInTestHarness);
        }
    }

    public class ShortcutIcon : TestHarnessItem
    {
        public ShortcutIcon(ReferencedFile referencedFile)
            : base(referencedFile, "link", false)
        {
            Attributes.Add("rel", "shortcut icon");
            Attributes.Add("type", "image/png");
            Attributes.Add("href", referencedFile.PathForUseInTestHarness);
        }
    }

    public class Script : TestHarnessItem
    {
        public Script(ReferencedFile referencedFile)
            : base(referencedFile, "script", true)
        {
            Attributes.Add("type", "text/javascript");
            Attributes.Add("src", referencedFile.PathForUseInTestHarness);
        }

        public Script(string scriptCode)
            : base(scriptCode, "script", true)
        {
            Attributes.Add("type", "text/javascript");
        }
    }

    public class Html : TestHarnessItem
    {
        const string scriptTagWrapper = @"<script id=""{0}"" type=""{1}"">{2}</script>";

        private readonly string contents;

        public Html(ReferencedFile referencedFile, IFileSystemWrapper fileSystem)
            : base(referencedFile, null, false)
        {
            contents = fileSystem.GetText(referencedFile.Path);

            if(referencedFile.TemplateOptions.Mode == TemplateMode.Script)
            {
                contents = string.Format(scriptTagWrapper, referencedFile.TemplateOptions.Id, referencedFile.TemplateOptions.Type, contents);
            }
        }

        public override string ToString()
        {
            return contents;
        }
    }
}
