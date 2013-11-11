using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.FileGenerator;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts
{
    public class TestContextBuilderFacts
    {
        // Shim to be able to preserve the old tests despite TestContextBuilder not
        // having a static GetScriptStatement method anymore.
        private static string TestContextBuilder_GetScriptStatement(string path)
        {
            return new Script(new ReferencedFile { Path = path }).ToString();
        }

        // Shim to be able to preserve the old tests despite TestContextBuilder not
        // having a static GetStyleStatement method anymore.
        private static string TestContextBuilder_GetStyleStatement(string path)
        {
            return new ExternalStylesheet(new ReferencedFile { Path = path }).ToString();
        }

        private class TestableTestContextBuilder : Testable<TestContextBuilder>
        {
            static int counter = 0;
            public List<ReferencedFile> ReferenceFiles { get; set; }
            public ChutzpahTestSettingsFile ChutzpahTestSettingsFile { get; set; }
            public TestableTestContextBuilder()
            {
                ReferenceFiles = new List<ReferencedFile>();
                var frameworkMock = Mock<IFrameworkDefinition>();
                frameworkMock.Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);
                frameworkMock.Setup(x => x.FrameworkKey).Returns("qunit");
                frameworkMock.Setup(x => x.TestRunner).Returns("qunitRunner.js");
                frameworkMock.Setup(x => x.TestHarness).Returns("qunit.html");
                frameworkMock.Setup(x => x.FileDependencies).Returns(new[] { "qunit.js", "qunit.css" });
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x => x);
                Mock<IFileProbe>().Setup(x => x.GetPathInfo(It.IsAny<string>())).Returns<string>(x => new PathInfo { FullPath = x, Type = PathType.JavaScript });
                Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder(It.IsAny<string>())).Returns(@"C:\temp\");
                Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns(string.Empty);
                Mock<IFileSystemWrapper>().Setup(x => x.GetRandomFileName()).Returns("unique");
                Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                Mock<IHasher>().Setup(x => x.Hash(It.IsAny<string>())).Returns("hash");
                Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo(@"TestFiles\qunit.html"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\qunit.js" });
                Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\qunit.js"))
                    .Returns(TestTempateContents);

                ChutzpahTestSettingsFile = new ChutzpahTestSettingsFile();
                Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(() => "settingsPath\\chutzpah.json" + counter++);
                Mock<IJsonSerializer>()
                    .Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>()))
                    .Returns(ChutzpahTestSettingsFile);


                Mock<IReferenceProcessor>()
                    .Setup(x => x.GetReferencedFiles(
                                It.IsAny<List<ReferencedFile>>(),
                                It.IsAny<IFrameworkDefinition>(),
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<ChutzpahTestSettingsFile>()))
                    .Callback<List<ReferencedFile>, IFrameworkDefinition, string, string, ChutzpahTestSettingsFile>(
                        (refs, def, text, path, settings) =>
                        {
                            refs.AddRange(ReferenceFiles);
                        });
            }
        }

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

        const string TestJSFileContents =
            @"/// <chutzpah_reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        /// <reference path=""../../js/style.css"" />
                        some javascript code
                        ";

        const string TestHtmlFileContents =
            @"/// <template path=""../../templates/file.html"" />
                        some javascript code
                        ";

        public class IsTestFile
        {
            [Fact]
            public void Will_return_false_if_test_file_is_null()
            {
                var creator = new TestableTestContextBuilder();

                var result = creator.ClassUnderTest.IsTestFile(null);

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_test_file_is_not_a_valid_file_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo { Type = PathType.Other });

                var result = creator.ClassUnderTest.IsTestFile("test.blah");

                Assert.False(result);
            }

            [Theory]
            [InlineData("test.ts", PathType.TypeScript)]
            [InlineData("test.coffee", PathType.CoffeeScript)]
            [InlineData("test.js", PathType.JavaScript)]
            [InlineData("test.html", PathType.Html)]
            public void Will_return_true_for_valid_files(string path, PathType type)
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(path)).Returns(new PathInfo { Type = type, FullPath = path });
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile(path);

                Assert.True(result);
            }

            [Fact]
            public void Will_return_false_if_test_file_does_not_exist()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = null });

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_test_framework_not_detected()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = null });
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(false);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.False(result);
            }

        }

        public class CleanContext
        {
            [Fact]
            public void Will_throw_if_context_is_null()
            {
                var builder = new TestableTestContextBuilder();

                Exception ex = Record.Exception(() => builder.ClassUnderTest.CleanupContext(null));

                Assert.IsType<ArgumentNullException>(ex);
            }

            [Fact]
            public void Will_delete_temporary_files()
            {
                var builder = new TestableTestContextBuilder();
                var context = new TestContext { TemporaryFiles = new string[] { "foo.js", "bar.js" } };

                builder.ClassUnderTest.CleanupContext(context);

                builder.Mock<IFileSystemWrapper>().Verify(x => x.DeleteFile("foo.js"));
                builder.Mock<IFileSystemWrapper>().Verify(x => x.DeleteFile("bar.js"));
            }

            [Fact]
            public void Will_suppress_temporary_file_deletion_errors()
            {
                var builder = new TestableTestContextBuilder();
                var context = new TestContext { TemporaryFiles = new string[] { "foo.js", "bar.js" } };
                builder.Mock<IFileSystemWrapper>().Setup(x => x.DeleteFile("foo.js")).Throws(new IOException());

                builder.ClassUnderTest.CleanupContext(context);

                builder.Mock<IFileSystemWrapper>().Verify(x => x.DeleteFile("bar.js"));
            }

        }

        public class BuildContext
        {
            [Fact]
            public void Will_throw_if_test_file_is_null()
            {
                var creator = new TestableTestContextBuilder();

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext((string)null, new TestOptions()));

                Assert.IsType<ArgumentNullException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_is_not_a_valid_test_type_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo { Type = PathType.Other });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.blah", new TestOptions()));

                Assert.IsType<ArgumentException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_does_not_exist()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = null });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.js", new TestOptions()));

                Assert.IsType<FileNotFoundException>(ex);
            }

            [Fact]
            public void Will_return_null_if_test_framework_not_determined()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\test.js" });
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(false);

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.Null(context);
            }

            [Fact]
            public void Will_save_generated_test_html_when_test_file_adjacent_and_return_path_for_js_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\folder\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\folder\_Chutzpah.hash.test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\folder\_Chutzpah.hash.test.html", context.TestHarnessPath);
                Assert.Equal(@"C:\folder\test.js", context.InputTestFile);
            }

            [Fact]
            public void Will_save_generated_test_html_when_settings_file_adjacent()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.TestHarnessLocationMode = TestHarnessLocationMode.SettingsFileAdjacent;
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\folder1\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.Equal(@"settingsPath\_Chutzpah.hash.test.html", context.TestHarnessPath);
            }

            [Fact]
            public void Will_save_generated_test_html_when_custom_placement()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.TestHarnessLocationMode = TestHarnessLocationMode.Custom;
                creator.ChutzpahTestSettingsFile.TestHarnessDirectory = "customFolder";
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath(It.Is<string>(p => p.Contains("customFolder")))).Returns("customFolder");
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\folder3\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.Equal(@"customFolder\_Chutzpah.hash.test.html", context.TestHarnessPath);
            }

            [Fact]
            public void Will_return_path_and_framework_for_html_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("testThing.html"))
                    .Returns(new PathInfo { Type = PathType.Html, FullPath = @"C:\testThing.html" });

                var context = creator.ClassUnderTest.BuildContext("testThing.html", new TestOptions());

                Assert.Equal(@"qunitRunner.js", context.TestRunner);
                Assert.Equal(@"C:\testThing.html", context.TestHarnessPath);
                Assert.Equal(@"C:\testThing.html", context.InputTestFile);
                Assert.False(context.IsRemoteHarness);
                Assert.Empty(context.ReferencedJavaScriptFiles);
            }

            [Fact]
            public void Will_return_path_and_framework_for_web_url()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("http://someUrl.com"))
                    .Returns(new PathInfo { Type = PathType.Url, FullPath = @"http://someUrl.com" });

                var context = creator.ClassUnderTest.BuildContext("http://someUrl.com", new TestOptions());

                Assert.Equal(@"qunitRunner.js", context.TestRunner);
                Assert.Equal(@"http://someUrl.com", context.TestHarnessPath);
                Assert.Equal(@"http://someUrl.com", context.InputTestFile);
                Assert.True(context.IsRemoteHarness);
                Assert.Empty(context.ReferencedJavaScriptFiles);
            }

            [Fact]
            public void Will_replace_test_dependency_placeholder_in_test_harness_html()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"path\_Chutzpah.hash.test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"TestFiles\qunit.js")).Returns(new PathInfo { FullPath = @"path\qunit.js" });
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"TestFiles\qunit.css")).Returns(new PathInfo { FullPath = @"path\qunit.css" });
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.js")).Returns(false);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\test.js")).Returns(TestJSFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                string scriptStatement = TestContextBuilder_GetScriptStatement(@"path\qunit.js");
                string cssStatement = TestContextBuilder_GetStyleStatement(@"path\qunit.css");
                Assert.Contains(scriptStatement, text);
                Assert.Contains(cssStatement, text);
                Assert.DoesNotContain("@@TestFrameworkDependencies@@", text);
            }

            [Fact]
            public void Will_set_js_test_file_to_file_under_test()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\test.js")).Returns(@"C:\path\test.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"C:\path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext(@"C:\test.js", new TestOptions());

                Assert.True(context.ReferencedJavaScriptFiles.SingleOrDefault(x => x.Path.Contains("test.js")).IsFileUnderTest);
            }

            [Fact]
            public void Will_pass_referenced_files_to_a_file_generator()
            {
                var creator = new TestableTestContextBuilder();
                var fileGenerator = new Mock<IFileGenerator>();
                creator.InjectArray(new[] { fileGenerator.Object });
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"C:\test.coffee")).Returns<string>(x => new PathInfo { FullPath = x, Type = PathType.CoffeeScript });

                var context = creator.ClassUnderTest.BuildContext(@"C:\test.coffee", new TestOptions());

                fileGenerator.Verify(x => x.Generate(It.IsAny<IEnumerable<ReferencedFile>>(), It.IsAny<List<string>>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }

            [Fact]
            public void Will_put_test_js_file_at_end_of_references_in_html_template_with_test_file()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"path\_Chutzpah.hash.test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\lib.js" });
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\common.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                string scriptStatement1 = TestContextBuilder_GetScriptStatement(@"path\lib.js");
                string scriptStatement2 = TestContextBuilder_GetScriptStatement(@"path\common.js");
                string scriptStatement3 = TestContextBuilder_GetScriptStatement(@"path\test.js");
                var pos1 = text.IndexOf(scriptStatement1);
                var pos2 = text.IndexOf(scriptStatement2);
                var pos3 = text.IndexOf(scriptStatement3);
                Assert.True(pos1 < pos2);
                Assert.True(pos2 < pos3);
                Assert.Equal(1, context.ReferencedJavaScriptFiles.Count(x => x.IsFileUnderTest));
            }

            [Fact]
            public void Will_put_test_html_file_at_end_of_references_in_html_template()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"path\_Chutzpah.hash.test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestHtmlFileContents);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\file.html"))
                    .Returns("<h1>This is the included HTML</h1>");
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\file.html" });


                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.Contains("<h1>This is the included HTML</h1>", text);
            }


            [Fact]
            public void Will_not_copy_referenced_file_if_it_is_the_test_runner()
            {
                var creator = new TestableTestContextBuilder();
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\qunit.js" });
                
                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true), Times.Never());
            }

            [Fact]
            public void Will_replace_referenced_js_file_place_holder_in_html_template_with_referenced_js_files_from_js_test_file()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"path\_Chutzpah.hash.test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\lib.js" });
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\common.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                string scriptStatement1 = TestContextBuilder_GetScriptStatement(@"path\lib.js");
                string scriptStatement2 = TestContextBuilder_GetScriptStatement(@"path\common.js");
                Assert.Contains(scriptStatement1, text);
                Assert.Contains(scriptStatement2, text);
                Assert.DoesNotContain("@@ReferencedJSFiles@@", text);
            }

            [Fact]
            public void Will_replace_referenced_css_file_place_holder_in_html_template_with_referenced_css_files_from_js_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"path\_Chutzpah.hash.test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\style.css" });
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                string styleStatement = TestContextBuilder_GetStyleStatement(@"path\style.css");
                Assert.Contains(styleStatement, text);
                Assert.DoesNotContain("@@ReferencedCSSFiles@@", text);
            }

            [Fact]
            public void Will_replace_custom_framework_placeholders_with_contents_from_framwork_definition()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;

                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"path\_Chutzpah.hash.test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);

                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(string.Empty);

                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });

                creator.Mock<IFrameworkDefinition>()
                    .Setup(x => x.GetFrameworkReplacements(It.IsAny<ChutzpahTestSettingsFile>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(new Dictionary<string, string>
                        {
                            {"CustomReference1", "CustomReplacement1"},
                            {"CustomReference2", "CustomReplacement2"}
                        });

                creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.DoesNotContain("@@CustomReference1@@", text);
                Assert.DoesNotContain("@@CustomReference2@@", text);
                Assert.Contains("CustomReplacement1", text);
                Assert.Contains("CustomReplacement2", text);
            }


            [Fact]
            public void Will_not_copy_referenced_path_if_not_a_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\test.js")).Returns(@"C:\path\test.js");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"C:\path\test.js"))
                    .Returns(TestFileContents);

                var context = creator.ClassUnderTest.BuildContext(@"C:\test.js", new TestOptions());

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(It.Is<string>(p => p.Contains("lib.js")), It.IsAny<string>(), true), Times.Never());
            }
        }

        public class GetAbsoluteFileUrl
        {
            // Shim to be able to preserve the old tests despite TestContextBuilder not
            // having a static GetAbsoluteFileUrl method anymore.
            private string TestContextBuilder_GetAbsoluteFileUrl(string path)
            {
                string html = new Script(new ReferencedFile { Path = path }).ToString();
                return Regex.Match(html, "src=\"([^\"]+)\"").Groups[1].Value;
            }

            [Fact]
            public void Will_prepend_scheme_and_convert_slashes_of_a_path_without_a_scheme_and_encode()
            {
                var actual = TestContextBuilder_GetAbsoluteFileUrl(@"D:\some\file\path.js");

                Assert.Equal("file:///D:/some/file/path.js", actual);
            }

            [Fact]
            public void Will_prepend_scheme_and_convert_slashes_of_a_path_containing_a_scheme_and_encode()
            {
                var actual = TestContextBuilder_GetAbsoluteFileUrl(@"D:\some\http://.js");

                Assert.Equal("file:///D:/some/http://.js", actual);
            }

            [Fact]
            public void Will_not_prefix_a_path_using_http_scheme()
            {
                var actual = TestContextBuilder_GetAbsoluteFileUrl("http://someurl/x.js");

                Assert.Equal("http://someurl/x.js", actual);
            }

            [Fact]
            public void Will_not_prefix_a_path_using_https_scheme()
            {
                var actual = TestContextBuilder_GetAbsoluteFileUrl("https://anyurl/y.js");

                Assert.Equal("https://anyurl/y.js", actual);
            }

            [Fact]
            public void Will_not_prefix_a_path_using_file_scheme()
            {
                var actual = TestContextBuilder_GetAbsoluteFileUrl("file://Z:/path/z.js");

                Assert.Equal("file://Z:/path/z.js", actual);
            }
        }
    }
}