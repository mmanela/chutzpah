using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Chutzpah.Models;

namespace Chutzpah
{
    public class TestHarness
    {
        public IList<TestHarnessItem> TestFrameworkDependencies { get; private set; }
        public IList<TestHarnessItem> ReferencedScripts { get; private set; }
        public IList<TestHarnessItem> ReferencedStyles { get; private set; }

        public TestHarness(IEnumerable<ReferencedFile> referencedFiles)
        {
            BuildTags(referencedFiles);
            CleanupTestHarness();
        }

        public string CreateHtmlText(string testHtmlTemplate)
        {
            var testJsReplacement = new StringBuilder();
            var testFrameworkDependencies = new StringBuilder();
            var referenceJsReplacement = new StringBuilder();
            var referenceCssReplacement = new StringBuilder();

            BuildReferenceHtml(testFrameworkDependencies,
                               referenceCssReplacement,
                               testJsReplacement,
                               referenceJsReplacement);

            testHtmlTemplate = testHtmlTemplate.Replace("@@TestFrameworkDependencies@@", testFrameworkDependencies.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@TestJSFile@@", testJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedJSFiles@@", referenceJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedCSSFiles@@", referenceCssReplacement.ToString());

            return testHtmlTemplate;
        }

        private void BuildTags(IEnumerable<ReferencedFile> referencedFilePaths)
        {
            ReferencedScripts = new List<TestHarnessItem>();
            ReferencedStyles = new List<TestHarnessItem>();
            TestFrameworkDependencies = new List<TestHarnessItem>();

            foreach (ReferencedFile referencedFile in referencedFilePaths)
            {
                string referencePath = string.IsNullOrEmpty(referencedFile.GeneratedFilePath)
                                        ? referencedFile.Path
                                        : referencedFile.GeneratedFilePath;
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
            }
        }

        private IList<TestHarnessItem> ChooseRefList(ReferencedFile referencedFile, string referencePath)
        {
            IList<TestHarnessItem> list = null;
            if (referencedFile.IsTestFrameworkDependency)
            {
                list = TestFrameworkDependencies;
            }
            else if (referencePath.EndsWith(Constants.CssExtension, StringComparison.OrdinalIgnoreCase))
            {
                list = ReferencedStyles;
            }
            else if (referencePath.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase))
            {
                list = ReferencedScripts;
            }
            return list;
        }

        private void BuildReferenceHtml(StringBuilder testFrameworkDependencies,
                                        StringBuilder referenceCssReplacement,
                                        StringBuilder testJsReplacement,
                                        StringBuilder referenceJsReplacement)
        {
            foreach (TestHarnessItem item in TestFrameworkDependencies)
            {
                testFrameworkDependencies.AppendLine(item.ToString());
            }
            foreach (TestHarnessItem item in ReferencedScripts)
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
        }


        private void CleanupTestHarness()
        {
            // Remove additional references to QUnit.
            // (Iterate over a copy to avoid concurrent modification of the list!)
            foreach (TestHarnessItem reference in ReferencedScripts.Where(r => r.HasFile).ToList())
            {
                if (reference.ReferencedFile.IsFileUnderTest) continue;

                var lastSlash = reference.ReferencedFile.Path.LastIndexOfAny(new[] { '/', '\\' });
                string fileName = reference.ReferencedFile.Path.Substring(lastSlash + 1);
                if (Regex.IsMatch(fileName, "^qunit(-[0-9]+\\.[0-9]+\\.[0-9]+)?\\.js$", RegexOptions.IgnoreCase))
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
                builder.AppendFormat(@" {0}=""{1}""", entry.Key, entry.Value);
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

        protected static string GetAbsoluteFileUrl(ReferencedFile referencedFile)
        {
            string referencePath = string.IsNullOrEmpty(referencedFile.GeneratedFilePath)
                        ? referencedFile.Path
                        : referencedFile.GeneratedFilePath;

            if (!RegexPatterns.SchemePrefixRegex.IsMatch(referencePath))
            {
                // Encode the reference path and then decode / (forward slash) and \ (back slash) into / (forward slash)
                return "file:///" + FileProbe.EncodeFilePath(referencePath);
            }

            return referencePath;
        }
    }

    public class ExternalStylesheet : TestHarnessItem
    {
        public ExternalStylesheet(ReferencedFile referencedFile) : base(referencedFile, "link", false)
        {
            Attributes.Add("rel", "stylesheet");
            Attributes.Add("type", "text/css");
            Attributes.Add("href", GetAbsoluteFileUrl(referencedFile));
        }
    }

    public class ShortcutIcon : TestHarnessItem
    {
        public ShortcutIcon(ReferencedFile referencedFile) : base(referencedFile, "link", false)
        {
            Attributes.Add("rel", "shortcut icon");
            Attributes.Add("type", "image/png");
            Attributes.Add("href", GetAbsoluteFileUrl(referencedFile));
        }
    }

    public class Script : TestHarnessItem
    {
        public Script(ReferencedFile referencedFile)
            : base(referencedFile, "script", true)
        {
            Attributes.Add("type", "text/javascript");
            Attributes.Add("src", GetAbsoluteFileUrl(referencedFile));
        }

        public Script(string scriptCode)
            : base(scriptCode, "script", true)
        {
            Attributes.Add("type", "text/javascript");
        }
    }

}
