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
            foreach (ReferencedFile referencedFile in referencedFilePaths)
            {
                string referencePath = string.IsNullOrEmpty(referencedFile.GeneratedFilePath)
                                        ? referencedFile.Path
                                        : referencedFile.GeneratedFilePath;

                if (referencePath.EndsWith(Constants.CssExtension, StringComparison.OrdinalIgnoreCase))
                {
                    ReferencedStyles.Add(new ExternalStylesheet(referencedFile));
                }
                else if (referencePath.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    ReferencedScripts.Add(new Script(referencedFile));
                }
            }
        }

        private string FillTestHtmlTemplate(string testHtmlTemplate,
                                            string inputTestFileDir)
        {
            var testJsReplacement = new StringBuilder();
            var referenceJsReplacement = new StringBuilder();
            var referenceCssReplacement = new StringBuilder();
            BuildReferenceHtml(referenceCssReplacement, testJsReplacement, referenceJsReplacement);

            testHtmlTemplate = testHtmlTemplate.Replace("@@TestJSFile@@", testJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@TestJSFileDir@@", inputTestFileDir);
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedJSFiles@@", referenceJsReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedCSSFiles@@", referenceCssReplacement.ToString());

            return testHtmlTemplate;
        }

        private void BuildReferenceHtml(StringBuilder referenceCssReplacement,
                                        StringBuilder testJsReplacement,
                                        StringBuilder referenceJsReplacement)
        {
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

            if (!RegexPatterns.SchemePrefixRegex.IsMatch(referencePath) && Path.IsPathRooted(referencePath))
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
