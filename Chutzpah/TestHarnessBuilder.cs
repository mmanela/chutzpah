using System.Collections.Generic;
using System.IO;
using Chutzpah.Coverage;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public interface ITestHarnessBuilder
    {
        void CreateTestHarness(TestContext testContext, TestOptions options)
    }

    public class TestHarnessBuilder : ITestHarnessBuilder
    {
        private readonly IFileProbe fileProbe;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IHasher hasher;

        public TestHarnessBuilder(
            IFileSystemWrapper fileSystem,
            IFileProbe fileProbe,
            IHasher hasher)
        {
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

            string testFilePathHash = hasher.Hash(testContext.InputTestFile);

            string testHtmlFilePath = Path.Combine(testContext.TestHarnessDirectory, string.Format(Constants.ChutzpahTemporaryFileFormat, testFilePathHash, "test.html"));
            testContext.TemporaryFiles.Add(testHtmlFilePath);

            var templatePath = GetTestHarnessTemplatePath(testContext.FrameworkDefinition, testContext.TestFileSettings);

            string testHtmlTemplate = fileSystem.GetText(templatePath);

            var harness = new TestHarness(testContext.TestFileSettings, options, testContext.ReferencedFiles, fileSystem);

            if (testContext.CoverageEngine != null)
            {
                testContext.CoverageEngine.PrepareTestHarnessForCoverage(harness, testContext.FrameworkDefinition, testContext.TestFileSettings);
            }

            string testFileContents = fileSystem.GetText(testContext.InputTestFile);
            var frameworkReplacements = testContext.FrameworkDefinition.GetFrameworkReplacements(testContext.TestFileSettings, testContext.InputTestFile, testFileContents)
                                        ?? new Dictionary<string, string>();

            string testHtmlText = harness.CreateHtmlText(testHtmlTemplate, frameworkReplacements);
            fileSystem.Save(testHtmlFilePath, testHtmlText);
            testContext.TestHarnessPath = testHtmlFilePath;
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