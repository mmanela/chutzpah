using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Chutzpah.Wrappers;
using Chutzpah.Models;
using Chutzpah.FileProcessors;

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
            var fileKind = GetTestFileType(file);
            if (fileKind == TestFileType.Other)
                throw new ArgumentException("Expecting a .js or .html file");
            stagingFolder = string.IsNullOrEmpty(stagingFolder) ? fileSystem.GetTemporayFolder() : stagingFolder;

            string filePath = fileProbe.FindPath(file);
            if (filePath == null)
                throw new FileNotFoundException("Unable to find file: " + file);

            if (!fileSystem.FolderExists(stagingFolder))
                fileSystem.CreateDirectory(stagingFolder);


            var testFileName = Path.GetFileName(file);
            var testFileText = fileSystem.GetText(filePath);
            var referencedFiles = GetReferencedFiles(fileKind, testFileText, filePath, stagingFolder);

            if (fileKind == TestFileType.JavaScript)
            {
                var stagedFilePath = Path.Combine(stagingFolder, testFileName);
                referencedFiles.Add(new ReferencedFile { Path = filePath, StagedPath = stagedFilePath, IsLocal = true, IsFileUnderTest = true });
            }

            CopyReferencedFiles(referencedFiles);

            var qunitFilePath = Path.Combine(stagingFolder, "qunit.js");
            var qunitCssFilePath = Path.Combine(stagingFolder, "qunit.css");
            CreateIfDoesNotExist(qunitFilePath, "Chutzpah.TestFiles.qunit.js");
            CreateIfDoesNotExist(qunitCssFilePath, "Chutzpah.TestFiles.qunit.css");
            var testHtmlFilePath = CreateTestHarness(stagingFolder, referencedFiles);

            return new TestContext { InputTestFile = filePath, TestHarnessPath = testHtmlFilePath, ReferencedJavaScriptFiles = referencedFiles };
        }

        public TestContext BuildContext(string file)
        {
            return BuildContext(file, null);
        }

        private string CreateTestHarness(string stagingFolder, IEnumerable<ReferencedFile> referencedFiles)
        {
            var testHtmlFilePath = Path.Combine(stagingFolder, "test.html");
            var testHtmlTemplate = EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.testTemplate.html");
            string testHtmlText = FillTestHtmlTemplate(testHtmlTemplate, referencedFiles);
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

        private IList<ReferencedFile> GetReferencedFiles(TestFileType testFileType, string testFileText, string testFilePath, string stagingFolder)
        {
            var regex = testFileType == TestFileType.JavaScript ? JsReferencePathRegex : HtmlReferencePathRegex;
            var files = new List<ReferencedFile>();
            foreach (Match match in regex.Matches(testFileText))
            {
                if (match.Success)
                {
                    string referencePath = match.Groups["Path"].Value;
                    Uri referenceUri = new Uri(referencePath, UriKind.RelativeOrAbsolute);
                    if (!referenceUri.IsAbsoluteUri || referenceUri.IsFile)
                    {
                        string relativeReferencePath = Path.Combine(Path.GetDirectoryName(testFilePath), referencePath);
                        var absolutePath = fileProbe.FindPath(relativeReferencePath);
                        if (absolutePath != null)
                        {
                            var uniqueFileName = MakeUniqueIfNeeded(Path.GetFileName(referencePath), stagingFolder);
                            var stagedPath = Path.Combine(stagingFolder, uniqueFileName);
                            fileSystem.CopyFile(absolutePath, stagedPath);
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

                    foreach (var referencedFileProcessor in referencedFileProcessors)
                    {
                        referencedFileProcessor.Process(referencedFile);
                    }
                    
                }
            }
        }

        private static string FillTestHtmlTemplate(string testHtmlTemplate, IEnumerable<ReferencedFile> referencedFiles)
        {
            var referenceReplacement = new StringBuilder();
            foreach (ReferencedFile referencedFile in referencedFiles)
            {
                var referencePath = referencedFile.IsLocal ? Path.GetFileName(referencedFile.StagedPath) : referencedFile.StagedPath;
                referenceReplacement.AppendLine(GetScriptStatement(referencePath));
            }

            testHtmlTemplate = testHtmlTemplate.Replace("@@ReferencedFiles@@", referenceReplacement.ToString());


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

        private static TestFileType GetTestFileType(string fileName)
        {
            if (IsHtmlFile(fileName)) return TestFileType.Html;
            if (IsJavaScriptFile(fileName)) return TestFileType.JavaScript;
            return TestFileType.Other;
        }

        private static bool IsHtmlFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null &&
                   (ext.Equals(".html", StringComparison.OrdinalIgnoreCase) || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsJavaScriptFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return ext != null && ext.Equals(".js", StringComparison.OrdinalIgnoreCase);
        }
    }
}