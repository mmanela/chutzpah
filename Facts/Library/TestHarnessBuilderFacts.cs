using System.Collections.Generic;
using System.Linq;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestHarnessBuilderFacts
    {
        private const string TestTempateContents = @"
<!DOCTYPE html><html><head>
    @@TestFrameworkDependencies@@
    @@ReferencedCSSFiles@@
    @@TestHtmlTemplateFiles@@
    @@ReferencedJSFiles@@
    @@TestJSFile@@
    @@CustomReplacement1@@
    @@CustomReplacement2@@
</head>
<body><div id=""qunit-fixture""></div></body></html>


";
        private static string TestContextBuilder_GetScriptStatement(string path)
        {
            return new Script(new ReferencedFile { Path = path, PathForUseInTestHarness = path }).ToString();
        }

        private static string TestContextBuilder_GetStyleStatement(string path)
        {
            return new ExternalStylesheet(new ReferencedFile { Path = path, PathForUseInTestHarness = path }).ToString();
        }


        private class TestableTestHarnessBuilder : Testable<TestHarnessBuilder>
        {
            public TestableTestHarnessBuilder()
            {
                var frameworkMock = Mock<IFrameworkDefinition>();
                frameworkMock.Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);
                frameworkMock.Setup(x => x.FrameworkKey).Returns("qunit");
                frameworkMock.Setup(x => x.GetTestRunner(It.IsAny<ChutzpahTestSettingsFile>(), It.IsAny<TestOptions>())).Returns("qunitRunner.js");
                frameworkMock.Setup(x => x.GetTestHarness(It.IsAny<ChutzpahTestSettingsFile>())).Returns("qunit.html");
                frameworkMock.Setup(x => x.GetFileDependencies(It.IsAny<ChutzpahTestSettingsFile>())).Returns(new[] { "qunit.js", "qunit.css" });
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x => x);
                Mock<IFileProbe>().Setup(x => x.GetPathInfo(It.IsAny<string>())).Returns<string>(x => new PathInfo { FullPath = x, Type = PathType.JavaScript });
                Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder(It.IsAny<string>())).Returns(@"C:\temp\");
                Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns(string.Empty);
                Mock<IFileSystemWrapper>().Setup(x => x.GetRandomFileName()).Returns("unique");
                Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                Mock<IHasher>().Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");
                Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo(@"TestFiles\qunit.html"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\qunit.html" });
                Mock<IFileProbe>()
                    .Setup(x => x.BuiltInDependencyDirectory)
                    .Returns(@"dependencyPath\");
                Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"dependencyPath\qunit.html"))
                    .Returns(TestTempateContents);
            }

            public TestContext GetContext()
            {
                var context =  new TestContext
                {

                    InputTestFiles = new []{ @"C:\folder\test.js"},
                    TestHarnessDirectory = @"C:\folder",
                    FrameworkDefinition = Mock<IFrameworkDefinition>().Object,
                    TestFileSettings = new ChutzpahTestSettingsFile().InheritFromDefault()
                };

                context.TestFileSettings.SettingsFileDirectory = "settingsPath";
                return context;
            }

        }

        [Fact]
        public void Will_save_generated_test_html()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()));
            Assert.Equal(@"C:\folder\_Chutzpah.hash.test.html", context.TestHarnessPath);
            Assert.Equal(@"C:\folder\test.js", context.InputTestFiles.FirstOrDefault());
        }

        [Fact]
        public void Will_use_custom_template_path()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            string text = null;
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"dependencyPath\qunit.js", PathForUseInTestHarness = @"dependencyPath\qunit.js", IsTestFrameworkFile = true});
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"dependencyPath\qunit.css", PathForUseInTestHarness = @"dependencyPath\qunit.css", IsTestFrameworkFile = true });

            context.TestFileSettings.CustomTestHarnessPath = @"folder\customHarness.html";
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);
            creator.Mock<IFileProbe>()
                .Setup(x => x.FindFilePath(@"settingsPath\folder\customHarness.html"))
                .Returns(@"path\customHarness.html");
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetText(@"path\customHarness.html"))
                .Returns(TestTempateContents);

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            string scriptStatement = TestContextBuilder_GetScriptStatement(@"dependencyPath\qunit.js");
            string cssStatement = TestContextBuilder_GetStyleStatement(@"dependencyPath\qunit.css");
            Assert.Contains(scriptStatement, text);
            Assert.Contains(cssStatement, text);
            Assert.DoesNotContain("@@TestFrameworkDependencies@@", text);
        }

        [Fact]
        public void Will_replace_test_dependency_placeholder_in_test_harness_html()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"dependencyPath\qunit.js", PathForUseInTestHarness = @"dependencyPath\qunit.js", IsTestFrameworkFile = true });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"dependencyPath\qunit.css", PathForUseInTestHarness = @"dependencyPath\qunit.css", IsTestFrameworkFile = true });

            string text = null;
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            string scriptStatement = TestContextBuilder_GetScriptStatement(@"dependencyPath\qunit.js");
            string cssStatement = TestContextBuilder_GetStyleStatement(@"dependencyPath\qunit.css");
            Assert.Contains(scriptStatement, text);
            Assert.Contains(cssStatement, text);
            Assert.DoesNotContain("@@TestFrameworkDependencies@@", text);
        }

        [Fact]
        public void Will_put_test_html_file_at_end_of_references_in_html_template()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\file.html" , PathForUseInTestHarness = @"path\file.html" });
            string text = null;
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetText(@"path\file.html"))
                .Returns("<h1>This is the included HTML</h1>");


            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            Assert.Contains("<h1>This is the included HTML</h1>", text);
        }

        [Fact]
        public void Will_put_test_js_file_at_end_of_references_in_html_template_with_test_file()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\test.js", PathForUseInTestHarness = @"path\test.js", IsFileUnderTest = true });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\lib.js" , PathForUseInTestHarness = @"path\lib.js" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\common.js" , PathForUseInTestHarness = @"path\common.js" });
            string text = null;
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            string scriptStatement1 = TestContextBuilder_GetScriptStatement(@"path\lib.js");
            string scriptStatement2 = TestContextBuilder_GetScriptStatement(@"path\common.js");
            string scriptStatement3 = TestContextBuilder_GetScriptStatement(@"path\test.js");
            var pos1 = text.IndexOf(scriptStatement1);
            var pos2 = text.IndexOf(scriptStatement2);
            var pos3 = text.IndexOf(scriptStatement3);
            Assert.True(pos1 < pos2);
            Assert.True(pos2 < pos3);
            Assert.Equal(1, context.ReferencedFiles.Count(x => x.IsFileUnderTest));
        }

        [Fact]
        public void Will_replace_referenced_js_file_place_holder_in_html_template_with_referenced_js_files_from_js_test_file()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\lib.js", PathForUseInTestHarness = @"path\lib.js" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\common.js", PathForUseInTestHarness = @"path\common.js" });
            string text = null;
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            string scriptStatement1 = TestContextBuilder_GetScriptStatement(@"path\lib.js");
            string scriptStatement2 = TestContextBuilder_GetScriptStatement(@"path\common.js");
            Assert.Contains(scriptStatement1, text);
            Assert.Contains(scriptStatement2, text);
            Assert.DoesNotContain("@@ReferencedJSFiles@@", text);
        }

        [Fact]
        public void Will_replace_referenced_css_file_place_holder_in_html_template_with_referenced_css_files_from_js_test_file()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"path\style.css", PathForUseInTestHarness = @"path\style.css" });
            string text = null;
            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            string styleStatement = TestContextBuilder_GetStyleStatement(@"path\style.css");
            Assert.Contains(styleStatement, text);
            Assert.DoesNotContain("@@ReferencedCSSFiles@@", text);
        }

        [Fact]
        public void Will_replace_custom_framework_placeholders_with_contents_from_framwork_definition()
        {
            var creator = new TestableTestHarnessBuilder();
            var context = creator.GetContext();
            string text = null;

            creator.Mock<IFileSystemWrapper>()
                .Setup(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()))
                .Callback<string, string>((x, y) => text = y);

            creator.Mock<IFrameworkDefinition>()
                .Setup(x => x.GetFrameworkReplacements(It.IsAny<ChutzpahTestSettingsFile>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string>
                        {
                            {"CustomReference1", "CustomReplacement1"},
                            {"CustomReference2", "CustomReplacement2"}
                        });

            creator.ClassUnderTest.CreateTestHarness(context, new TestOptions());

            Assert.DoesNotContain("@@CustomReference1@@", text);
            Assert.DoesNotContain("@@CustomReference2@@", text);
            Assert.Contains("CustomReplacement1", text);
            Assert.Contains("CustomReplacement2", text);
        }


    }
}