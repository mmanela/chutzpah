using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.FileConverter;
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
        private class TestableTestContextBuilder : Testable<TestContextBuilder>
        {
            public TestableTestContextBuilder()
            {
                var frameworkMock = Mock<IFrameworkDefinition>();
                frameworkMock.Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);
                frameworkMock.Setup(x => x.TestRunner).Returns("qunitRunner.js");
                frameworkMock.Setup(x => x.TestHarness).Returns("qunit.html");
                frameworkMock.Setup(x => x.FileDependencies).Returns(new[] { "qunit.js" });
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
            }
        }

        public static string QUnitContents
        {
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.js"); }
        }

        public static string QUnitCssContents
        {
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.css"); }
        }

        private const string TestTempateContents = @"
<!DOCTYPE html><html><head>
    @@ReferencedCSSFiles@@
    @@ReferencedJSFiles@@
    @@TestJSFile@@
    DIR:@@TestJSFileDir@@

</head>
<body><div id=""qunit-fixture""></div></body></html>


";

        const string TestJSFileContents =
            @"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        /// <reference path=""../../js/style.css"" />
                        some javascript code
                        ";

        const string TestJSFileWithReferencesContents =
            @"/// <reference path=""../../js/references.js"" />
                        some javascript code
                        ";
        const string ReferencesFile =
            @"/// <reference path=""lib.js"" />";

        const string ReferencesFileInfiniteLoop =
            @"/// <reference path=""../../js/references.js"" />";



        const string TestJSFileWithQUnitContents =
            @"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        /// <reference path=""../../js/qunit.js"" />
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
            public void Will_return_false_test_file_is_not_a_js_or_coffee_or_html_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo { Type = PathType.Other });

                var result = creator.ClassUnderTest.IsTestFile("test.blah");

                Assert.False(result);
            }

            [Theory]
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
                var context = new TestContext {TemporaryFiles = new string[] {"foo.js", "bar.js"}};

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

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext((string)null));

                Assert.IsType<ArgumentNullException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_is_not_a_js_or_html_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo { Type = PathType.Other });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.blah"));

                Assert.IsType<ArgumentException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_does_not_exist()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = null });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.js"));

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

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.Null(context);
            }

            [Fact]
            public void Will_save_generated_test_html_file_to_temporary_folder_and_return_path_for_js_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IHasher>().Setup(x => x.Hash(@"C:\test.js")).Returns("test.JS_hash");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder("test.JS_hash")).Returns(@"C:\temp2\");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp2\test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\temp2\test.html", context.TestHarnessPath);
                Assert.Equal(@"C:\test.js", context.InputTestFile);
            }

            [Fact]
            public void Will_return_path_and_framework_for_html_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("testThing.html"))
                    .Returns(new PathInfo { Type = PathType.Html, FullPath = @"C:\testThing.html" });

                var context = creator.ClassUnderTest.BuildContext("testThing.html");

                Assert.Equal(@"qunitRunner.js", context.TestRunner);
                Assert.Equal(@"C:\testThing.html", context.TestHarnessPath);
                Assert.Equal(@"C:\testThing.html", context.InputTestFile);
                Assert.Empty(context.ReferencedJavaScriptFiles);
            }


            [Fact]
            public void Will_copy_test_dependency_to_temporary_folder_if_doesnt_exist()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"TestFiles\qunit.js")).Returns(new PathInfo { FullPath = @"path\qunit.js" });
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.js")).Returns(false);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"pa\test.js")).Returns(TestJSFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true));
            }

            [Fact]
            public void Will_copy_test_dependency_to_temporary_folder_if_newer()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"TestFiles\qunit.js")).Returns(new PathInfo { FullPath = @"path\qunit.js" });
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.js")).Returns(true);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"pa\test.js")).Returns(TestJSFileContents);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"path\qunit.js")).Returns(DateTime.Now);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\temp\qunit.js")).Returns(DateTime.Now.AddDays(-1));

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true));
            }

            [Fact]
            public void Will_not_copy_test_dependency_to_temporary_folder_if_not_newer()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"TestFiles\qunit.js")).Returns(new PathInfo { FullPath = @"path\qunit.js" });
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.js")).Returns(true);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"pa\test.js")).Returns(TestJSFileContents);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"path\qunit.js")).Returns(DateTime.Now);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\temp\qunit.js")).Returns(DateTime.Now.AddDays(1));

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true), Times.Never());
            }


            [Fact]
            public void Will_set_js_test_file_to_file_under_test()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.True(context.ReferencedJavaScriptFiles.SingleOrDefault(x => x.Path.Contains("test.js")).IsFileUnderTest);
            }

            [Fact]
            public void Will_pass_referenced_files_to_coffeescriptconverter()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo(@"test.coffee")).Returns<string>(x => new PathInfo { FullPath = x, Type = PathType.CoffeeScript });

                var context = creator.ClassUnderTest.BuildContext("test.coffee");

                creator.Mock<ICoffeeScriptFileConverter>().Verify(x => x.Convert(It.IsAny<ReferencedFile>(), It.IsAny<List<string>>()));
            }

            [Fact(Timeout = 5000)]
            public void Will_stop_infinite_loop_when_processing_referenced_files()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithReferencesContents);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(ReferencesFileInfiniteLoop);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.NotNull(context);
            }

            [Fact]
            public void Will_not_copy_referenced_file_if_it_is_the_test_runner()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithQUnitContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"qunit.js")))
                    .Returns(@"path\qunit.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true), Times.Never());
            }

            [Fact]
            public void Will_put_recursively_referenced_files_before_parent_file_in_test_harness()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileWithReferencesContents);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(ReferencesFile);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"path\lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"path\references.js");
                string scriptStatement3 = TestContextBuilder.GetScriptStatement(@"path\test.js");
                var pos1 = text.IndexOf(scriptStatement1);
                var pos2 = text.IndexOf(scriptStatement2);
                var pos3 = text.IndexOf(scriptStatement3);
                Assert.True(pos1 < pos2);
                Assert.True(pos2 < pos3);

            }

            [Fact]
            public void Will_put_test_js_file_at_end_of_references_in_html_template_with_test_file()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"path\lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"path\common.js");
                string scriptStatement3 = TestContextBuilder.GetScriptStatement(@"path\test.js");
                var pos1 = text.IndexOf(scriptStatement1);
                var pos2 = text.IndexOf(scriptStatement2);
                var pos3 = text.IndexOf(scriptStatement3);
                Assert.True(pos1 < pos2);
                Assert.True(pos2 < pos3);
                Assert.Equal(1, context.ReferencedJavaScriptFiles.Count(x => x.IsFileUnderTest));
            }

            [Fact]
            public void Will_put_normalzed_test_js_dir_in_html_template()
            {
                var creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"this\path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"this\path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.True(text.Contains("DIR:this/path"));
            }

            [Fact]
            public void Will_replace_referenced_js_file_place_holder_in_html_template_with_referenced_css_files_from_js_test_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"path\lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"path\common.js");
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
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(@"path\../../js/style.css"))
                    .Returns(@"path\style.css");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.js")).Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string styleStatemenet = TestContextBuilder.GetStyleStatement(@"path\style.css");
                Assert.Contains(styleStatemenet, text);
                Assert.DoesNotContain("@@ReferencedCSSFiles@@", text);
            }

            [Fact]
            public void Will_not_copy_referenced_path_if_not_a_file()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(It.Is<string>(p => p.Contains("lib.js")), It.IsAny<string>(), true), Times.Never());
            }

            [Fact]
            public void Will_replace_referenced_file_place_holder_with_referenced_uri()
            {
                TestableTestContextBuilder creator = new TestableTestContextBuilder();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"path\test.js" });
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement = TestContextBuilder.GetScriptStatement(@"http://a.com/lib.js");
                Assert.Contains(scriptStatement, text);
            }
        }

        public class GetAbsoluteFileUrl
        {
            [Fact]
            public void Will_prepend_scheme_and_convert_slashes_of_a_path_without_a_scheme()
            {
                var actual = TestContextBuilder.GetAbsoluteFileUrl(@"D:\some\file\path.js");

                Assert.Equal("file:///D:/some/file/path.js", actual);
            }

            [Fact]
            public void Will_prepend_scheme_and_convert_slashes_of_a_path_containing_a_scheme()
            {
                var actual = TestContextBuilder.GetAbsoluteFileUrl(@"D:\some\http://.js");

                Assert.Equal("file:///D:/some/http://.js", actual);
            }

            [Fact]
            public void Will_not_prefix_a_path_using_http_scheme()
            {
                var actual = TestContextBuilder.GetAbsoluteFileUrl("http://someurl/x.js");

                Assert.Equal("http://someurl/x.js", actual);
            }

            [Fact]
            public void Will_not_prefix_a_path_using_https_scheme()
            {
                var actual = TestContextBuilder.GetAbsoluteFileUrl("https://anyurl/y.js");

                Assert.Equal("https://anyurl/y.js", actual);
            }

            [Fact]
            public void Will_not_prefix_a_path_using_file_scheme()
            {
                var actual = TestContextBuilder.GetAbsoluteFileUrl("file://Z:/path/z.js");

                Assert.Equal("file://Z:/path/z.js", actual);
            }
        }
    }
}