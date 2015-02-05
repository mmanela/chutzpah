using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Coverage;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public interface ITestHarnessBuilder
    {
        void CreateTestHarness(TestContext testContext, TestOptions options);
    }

    public class TestHarnessBuilder : ITestHarnessBuilder
    {
        private readonly IFileProbe fileProbe;
        private readonly IReferenceProcessor referenceProcessor;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IHasher hasher;
    
        public TestHarnessBuilder(
            IReferenceProcessor referenceProcessor,
            IFileSystemWrapper fileSystem,
            IFileProbe fileProbe,
            IHasher hasher)
        {
            this.referenceProcessor = referenceProcessor;
            this.fileSystem = fileSystem;
            this.fileProbe = fileProbe;
            this.hasher = hasher;
        }

        public void CreateTestHarness(TestContext testContext, TestOptions options)
        {
            if (!string.IsNullOrEmpty(testContext.TestHarnessPath))
            {
                // If we already have a test harness path then this means we executed on an existing html file or url
                // So we dont need to generate the harness
                return;
            }


            SetupAmdPathsIfNeeded(testContext.TestFileSettings, testContext.ReferencedFiles.ToList(), testContext.TestHarnessDirectory);

            string testFilePathHash = hasher.Hash(string.Join(",",testContext.InputTestFiles));

            string testHtmlFilePath = Path.Combine(testContext.TestHarnessDirectory, string.Format(Constants.ChutzpahTemporaryFileFormat, testFilePathHash, "test.html"));
            testContext.TemporaryFiles.Add(testHtmlFilePath);

            var templatePath = GetTestHarnessTemplatePath(testContext.FrameworkDefinition, testContext.TestFileSettings);

            string testHtmlTemplate = fileSystem.GetText(templatePath);

            var harness = new TestHarness(testContext.TestFileSettings, options, testContext.ReferencedFiles, fileSystem);

            if (testContext.CoverageEngine != null)
            {
                testContext.CoverageEngine.PrepareTestHarnessForCoverage(harness, testContext.FrameworkDefinition, testContext.TestFileSettings);
            }

            var kvps = testContext.ReferencedFiles
                .Where(x => x.IsFileUnderTest && x.ReferencedFiles.Any())
                .SelectMany(x => x.FrameworkReplacements);
            var frameworkReplacements = new Dictionary<string, string>();
            foreach (var pair in kvps)
            {
                frameworkReplacements[pair.Key] = pair.Value;
            }
                 
            string testHtmlText = harness.CreateHtmlText(testHtmlTemplate, frameworkReplacements);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            testContext.TestHarnessPath = testHtmlFilePath;
        }

        private void SetupAmdPathsIfNeeded(ChutzpahTestSettingsFile chutzpahTestSettings, List<ReferencedFile> referencedFiles, string testHarnessDirectory)
        {
            if (chutzpahTestSettings.TestHarnessReferenceMode == TestHarnessReferenceMode.AMD)
            {
                referenceProcessor.SetupAmdFilePaths(referencedFiles, testHarnessDirectory, chutzpahTestSettings);
            }
        }

        private string GetTestHarnessTemplatePath(IFrameworkDefinition definition, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            string templatePath = null;

            if (!string.IsNullOrEmpty(chutzpahTestSettings.CustomTestHarnessPath))
            {
                // If CustomTestHarnessPath is absolute path then Path.Combine just returns it
                var harnessPath = Path.Combine(chutzpahTestSettings.SettingsFileDirectory, chutzpahTestSettings.CustomTestHarnessPath);
                var fullPath = fileProbe.FindFilePath(harnessPath);
                if (fullPath != null)
                {
                    ChutzpahTracer.TraceInformation("Using Custom Test Harness from {0}", fullPath);
                    templatePath = fullPath;
                }
                else
                {
                    ChutzpahTracer.TraceError("Cannot find Custom Test Harness at {0}", chutzpahTestSettings.CustomTestHarnessPath);
                }
            }

            if (templatePath == null)
            {
                templatePath = fileProbe.GetPathInfo(Path.Combine(Constants.TestFileFolder, definition.GetTestHarness(chutzpahTestSettings))).FullPath;

                ChutzpahTracer.TraceInformation("Using builtin Test Harness from {0}", templatePath);
            }
            return templatePath;
        }
    }
}