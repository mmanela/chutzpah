using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Chutzpah.Models;

namespace Chutzpah
{
    public class TestHarness
    {
        private readonly string inputTestFilePath;

        public IList<HtmlTag> TestFrameworkDependencies { get; private set; }
        public IList<HtmlTag> ReferencedScripts { get; private set; }
        public IList<HtmlTag> ReferencedStyles { get; private set; }

        public TestHarness(string inputTestFilePath,
                             IEnumerable<ReferencedFile> referencedFiles)
        {
            this.inputTestFilePath = inputTestFilePath;
            BuildTags(referencedFiles);
        }

        public string CreateHtmlText(string testHtmlTemplate)
        {
            string inputTestFileDir = Path.GetDirectoryName(inputTestFilePath).Replace("\\", "/");
            string testHtmlText = FillTestHtmlTemplate(testHtmlTemplate, inputTestFileDir);
            return testHtmlText;
        }

        private void BuildTags(IEnumerable<ReferencedFile> referencedFilePaths)
        {
            ReferencedScripts = new List<HtmlTag>();
            ReferencedStyles = new List<HtmlTag>();
            TestFrameworkDependencies = new List<HtmlTag>();

            foreach (ReferencedFile referencedFile in referencedFilePaths)
            {
                string referencePath = string.IsNullOrEmpty(referencedFile.GeneratedFilePath)
                                        ? referencedFile.Path
                                        : referencedFile.GeneratedFilePath;
                IList<HtmlTag> refList = ChooseRefList(referencedFile, referencePath);
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

        private IList<HtmlTag> ChooseRefList(ReferencedFile referencedFile, string referencePath)
        {
            IList<HtmlTag> list = null;
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

        private string FillTestHtmlTemplate(string testHtmlTemplate,
                                            string inputTestFileDir)
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
            testHtmlTemplate = testHtmlTemplate.Replace("@@TestJSFileDir@@", inputTestFileDir);
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedJSFiles@@", referenceJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedCSSFiles@@", referenceCssReplacement.ToString());

            return testHtmlTemplate;
        }

        private void BuildReferenceHtml(StringBuilder testFrameworkDependencies,
                                        StringBuilder referenceCssReplacement,
                                        StringBuilder testJsReplacement,
                                        StringBuilder referenceJsReplacement)
        {
            foreach (HtmlTag tag in TestFrameworkDependencies)
            {
                testFrameworkDependencies.AppendLine(tag.ToString());
            }
            foreach (HtmlTag tag in ReferencedScripts)
            {
                if (tag.ReferencedFile != null && tag.ReferencedFile.IsFileUnderTest)
                {
                    testJsReplacement.AppendLine(tag.ToString());
                }
                else
                {
                    referenceJsReplacement.AppendLine(tag.ToString());
                }
            }
            foreach (HtmlTag tag in ReferencedStyles)
            {
                referenceCssReplacement.AppendLine(tag.ToString());
            }
        }
    }

    public class HtmlTag
    {
        private readonly bool explicitEndTag;
        private string contents;
        public string TagName { get; private set; }
        public IDictionary<string, string> Attributes { get; private set; }
        public ReferencedFile ReferencedFile { get; private set; }

        public HtmlTag(ReferencedFile referencedFile, string tagName, bool explicitEndTag)
            : this(tagName, explicitEndTag)
        {
            ReferencedFile = referencedFile;
        }

        public HtmlTag(string contents, string tagName, bool explicitEndTag) : this(tagName, explicitEndTag)
        {
            this.contents = contents;
        }

        private HtmlTag(string tagName, bool explicitEndTag)
        {
            TagName = tagName;
            this.explicitEndTag = explicitEndTag;
            Attributes = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("<");
            builder.Append(TagName);
            foreach (var entry in Attributes)
            {
                builder.AppendFormat(@" {0}=""{1}""", entry.Key, entry.Value);
            }
            if (explicitEndTag || contents != null)
            {
                builder.AppendFormat(">{1}</{0}>", TagName, contents ?? "");
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
                return "file:///" + referencePath.Replace('\\', '/');
            }

            return referencePath;
        }

    }

    public class ExternalStylesheet : HtmlTag
    {
        public ExternalStylesheet(ReferencedFile referencedFile) : base(referencedFile, "link", false)
        {
            Attributes.Add("rel", "stylesheet");
            Attributes.Add("type", "text/css");
            Attributes.Add("href", GetAbsoluteFileUrl(referencedFile));
        }
    }

    public class ShortcutIcon : HtmlTag
    {
        public ShortcutIcon(ReferencedFile referencedFile) : base(referencedFile, "link", false)
        {
            Attributes.Add("rel", "shortcut icon");
            Attributes.Add("type", "image/png");
            Attributes.Add("href", GetAbsoluteFileUrl(referencedFile));
        }
    }

    public class Script : HtmlTag
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
