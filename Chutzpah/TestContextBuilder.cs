using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Chutzpah.Wrappers;
using Chutzpah.Models;
using Chutzpah.FileProcessors;
using HtmlAgilityPack;

namespace Chutzpah
{

    public class TestContextBuilder : ITestContextBuilder
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly IFileProbe fileProbe;
        IEnumerable<IReferencedFileProcessor> referencedFileProcessors;

        private readonly Regex JsReferencePathRegex = new Regex(@"^\s*///\s*<\s*reference\s+path\s*=\s*[""""'](?<Path>[^""""<>|]+)[""""']\s*/>",
                                                              RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex HtmlReferencePathRegex = new Regex(@"^\s*<\s*script\s*.*?src\s*=\s*[""""'](?<Path>[^""""<>|]+)[""""'].*?>", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Regex TestRunnerRegex = new Regex(@"^qunit.js$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string QUnitTestFixtureId = "qunit-fixture";

        public TestContextBuilder(IFileSystemWrapper fileSystem, IFileProbe fileProbe, IEnumerable<IReferencedFileProcessor> referencedFileProcessors)
        {
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.referencedFileProcessors = referencedFileProcessors;
        }

        public TestContext BuildContext(string file, string stagingFolder)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException("file");
            var fileKind = fileProbe.GetPathType(file);
            if (fileKind != PathType.JavaScript && fileKind != PathType.Html)
                throw new ArgumentException("Expecting a .js or .html file");
            stagingFolder = string.IsNullOrEmpty(stagingFolder) ? fileSystem.GetTemporayFolder() : stagingFolder;

            string filePath = fileProbe.FindFilePath(file);
            if (filePath == null)
                throw new FileNotFoundException("Unable to find file: " + file);

            if (!fileSystem.FolderExists(stagingFolder))
                fileSystem.CreateDirectory(stagingFolder);


            var testFileName = Path.GetFileName(file);
            var testFileText = fileSystem.GetText(filePath);
            var referencedFiles = GetReferencedFiles(fileKind, testFileText, filePath, stagingFolder);
            var fixtureContent = "";

            if (fileKind == PathType.JavaScript)
            {
                var stagedFilePath = Path.Combine(stagingFolder, testFileName);
                referencedFiles.Add(new ReferencedFile { Path = filePath, StagedPath = stagedFilePath, IsLocal = true, IsFileUnderTest = true });
            }
            else if(fileKind == PathType.Html)
            {
                fixtureContent = GetTextFixtureContent(testFileText);
            }

            CopyReferencedFiles(referencedFiles);

            var qunitFilePath = Path.Combine(stagingFolder, "qunit.js");
            CreateIfDoesNotExist(qunitFilePath, "Chutzpah.TestFiles.qunit.js");
            var qunitCssFilePath = Path.Combine(stagingFolder, "qunit.css");
            CreateIfDoesNotExist(qunitCssFilePath, "Chutzpah.TestFiles.qunit.css");

            var testHtmlFilePath = CreateTestHarness(stagingFolder, referencedFiles, fixtureContent);

            return new TestContext { InputTestFile = filePath, TestHarnessPath = testHtmlFilePath, ReferencedJavaScriptFiles = referencedFiles };
        }

        private string GetTextFixtureContent(string htmlHarnessText)
        {
            var fixtureContent = "";
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlHarnessText);

            var testFixture = htmlDocument.GetElementbyId(QUnitTestFixtureId);
            if(testFixture != null)
            {
                fixtureContent = testFixture.InnerHtml;
            }

            return fixtureContent;
        }

        public TestContext BuildContext(string file)
        {
            return BuildContext(file, null);
        }

        private string CreateTestHarness(string stagingFolder, IEnumerable<ReferencedFile> referencedFiles, string fixtureContent)
        {
            var testHtmlFilePath = Path.Combine(stagingFolder, "test.html");
            var testHtmlTemplate = EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.html");
            string testHtmlText = FillTestHtmlTemplate(testHtmlTemplate, referencedFiles, fixtureContent);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            return testHtmlFilePath;
        }

        private void CreateIfDoesNotExist(string filePath, string embeddedPath)
        {
            if (!fileSystem.FileExists(filePath))
            {
                using (var stream = EmbeddedManifestResourceReader.GetEmbeddedResoureStream<TestRunner>(embeddedPath))
                {
                    fileSystem.Save(filePath, stream);
                }
            }
        }

        private IList<ReferencedFile> GetReferencedFiles(PathType testFileType, string testFileText, string testFilePath, string stagingFolder)
        {
            var regex = testFileType == PathType.JavaScript ? JsReferencePathRegex : HtmlReferencePathRegex;
            var files = new List<ReferencedFile>();
            foreach (Match match in regex.Matches(testFileText))
            {
                if (match.Success)
                {
                    string referencePath = match.Groups["Path"].Value;
                    Uri referenceUri = new Uri(referencePath, UriKind.RelativeOrAbsolute);
                    var referenceFileName = Path.GetFileName(referencePath);

                    // Don't copy over test runner, since we use our own.
                    if (TestRunnerRegex.IsMatch(referenceFileName))
                    {
                        continue;
                    }

                    if (!referenceUri.IsAbsoluteUri || referenceUri.IsFile)
                    {
                        string relativeReferencePath = Path.Combine(Path.GetDirectoryName(testFilePath), referencePath);
                        var absolutePath = fileProbe.FindFilePath(relativeReferencePath);
                        if (absolutePath != null)
                        {
                            var uniqueFileName = MakeUniqueIfNeeded(referenceFileName, stagingFolder);
                            var stagedPath = Path.Combine(stagingFolder, uniqueFileName);
                            files.Add(new ReferencedFile { Path = absolutePath, StagedPath = stagedPath, IsLocal = true });
                        }
                    }
                    else if (referenceUri.IsAbsoluteUri)
                    {
                        files.Add(new ReferencedFile { Path = referencePath, StagedPath = referencePath, IsLocal = false });
                    }
                }
            }
            return files;
        }

        private void CopyReferencedFiles(IEnumerable<ReferencedFile> referencedFiles)
        {
            foreach (var referencedFile in referencedFiles)
            {
                if (referencedFile.IsLocal)
                {
                    fileSystem.CopyFile(referencedFile.Path, referencedFile.StagedPath);
                    fileSystem.SetFileAttributes(referencedFile.StagedPath, FileAttributes.Normal);

                    foreach (var referencedFileProcessor in referencedFileProcessors)
                    {
                        referencedFileProcessor.Process(referencedFile);
                    }

                }
            }
        }

        private static string FillTestHtmlTemplate(string testHtmlTemplate, IEnumerable<ReferencedFile> referencedFiles, string fixtureContent)
        {
            var referenceReplacement = new StringBuilder();
            foreach (ReferencedFile referencedFile in referencedFiles)
            {
                var referencePath = referencedFile.IsLocal ? Path.GetFileName(referencedFile.StagedPath) : referencedFile.StagedPath;
                referenceReplacement.AppendLine(GetScriptStatement(referencePath));
            }

            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedFiles@@", referenceReplacement.ToString());
            testHtmlTemplate = testHtmlTemplate.Replace("@@FixtureContent@@", fixtureContent);


            return testHtmlTemplate;
        }

        public static string GetScriptStatement(string path)
        {
            const string format = @"<script type=""text/javascript"" src=""{0}""></script>";
            return string.Format(format, path);
        }

        private string MakeUniqueIfNeeded(string fileName, string directoryPath)
        {
            var filePath = Path.Combine(directoryPath, fileName);
            if (fileSystem.FileExists(filePath))
            {
                var randomFileName = fileSystem.GetRandomFileName();
                return string.Format("{0}_{1}", randomFileName, fileName);
            }

            return fileName;
        }
    }
}