using System;
using System.IO;
using System.Linq;
using Chutzpah.Frameworks;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestContextBuilderFacts
    {
        private class TestableHtmlTestFileCreator : Testable<TestContextBuilder>
        {
            public TestableHtmlTestFileCreator()
            {
                IFrameworkDefinition qunitDefinition = new QUnitDefinition();
                Mock<IFrameworkManager>().Setup(x => x.TryDetectFramework(It.IsAny<string>(), out qunitDefinition)).Returns(true);
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x => x);
                Mock<IFileProbe>().Setup(x => x.GetPathType(It.IsAny<string>())).Returns(PathType.JavaScript);
                Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns(string.Empty);
                Mock<IFileSystemWrapper>().Setup(x => x.GetRandomFileName()).Returns("unique");
                Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
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

        public static string TestTempateContents
        {
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.qunit.html"); }
        }

        const string TestHTMLFileContents =
@"
<html>
    <head>
        <script type=""text/javascript"" src=""qunit.js""></script>
        <script type=""text/javascript"" src=""../../js/common.js""></script>
        <script type=""text/javascript"" src=""lib.js""></script>
    </head>
    <body>
        <div id=""qunit-fixture"">
            <div> some <a>fixture</a> content </div>
        </div>
    </body>
</html>
";
        const string TestJSFileContents =
@"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        some javascript code
                        ";

        const string TestJSFileWithQUnitContents =
@"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        /// <reference path=""../../js/qunit.js"" />
                        some javascript code
                        ";

        public class CreateTestFile_WithPath
        {
            [Fact]
            public void Will_create_test_folder_if_does_not_exists()
            {
                var creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(@"C:\existing")).Returns(false);

                var res = creator.ClassUnderTest.BuildContext("test.js", @"C:\existing");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CreateDirectory(@"C:\existing"));
            }
        }

        public class CreateTestFile
        {
            [Fact]
            public void Will_throw_if_test_file_is_null()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext(null));

                Assert.IsType<ArgumentNullException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_is_not_a_js_or_html_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathType("test.blah")).Returns(PathType.Other);

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.blah"));

                Assert.IsType<ArgumentException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_does_not_exist()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns((string)null);

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.js"));

                Assert.IsType<FileNotFoundException>(ex);
            }

            [Fact]
            public void Will_save_generated_test_html_file_to_temporary_folder_and_return_path_for_js_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"C:\test.js");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\temp\test.html", context.TestHarnessPath);
                Assert.Equal(@"C:\test.js", context.InputTestFile);
            }

            [Fact]
            public void Will_save_generated_test_html_file_to_temporary_folder_and_return_path_for_html_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("testThing.html")).Returns(@"C:\testThing.html");

                var context = creator.ClassUnderTest.BuildContext("testThing.html");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\temp\test.html", context.TestHarnessPath);
                Assert.Equal(@"C:\testThing.html", context.InputTestFile);
            }

            [Fact]
            public void Will_save_qunit_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.js")).Returns(false);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"pa\test.js")).Returns(TestJSFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\qunit.js", It.IsAny<Stream>()));
            }

            [Fact]
            public void Will_save_qunit_css_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\qunit.css")).Returns(false);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.Save(@"C:\temp\qunit.css", It.IsAny<Stream>()));
            }

            [Fact]
            public void Will_copy_test_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\test.js", @"C:\temp\test.js", true));
            }

            [Fact]
            public void Will_set_js_test_file_to_file_under_test()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext("test.js");

                Assert.True(context.ReferencedJavaScriptFiles.SingleOrDefault(x => x.Path.Contains("test.js")).IsFileUnderTest);
            }

            [Fact]
            public void Will_copy_files_referenced_from_test_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
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

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\lib.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\lib.js", FileAttributes.Normal));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\common.js", @"C:\temp\common.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.SetFileAttributes(@"C:\temp\common.js", FileAttributes.Normal));
            }

            [Fact]
            public void Will_not_copy_referenced_file_if_it_is_the_test_runner()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
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
            public void Will_run_referenced_files_through_referenced_file_processors()
            {
                var creator = new TestableHtmlTestFileCreator();
                var processor = new Mock<IReferencedFileProcessor>();
                creator.InjectArray<IReferencedFileProcessor>(new[] { processor.Object });
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\lib.js")).Returns(true);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\common.js")).Returns(true);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
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

                processor.Verify(x => x.Process(It.Is<ReferencedFile>(f => f.Path == @"path\lib.js" && f.StagedPath == @"C:\temp\unique_lib.js")));
                processor.Verify(x => x.Process(It.Is<ReferencedFile>(f => f.Path == @"path\common.js" && f.StagedPath == @"C:\temp\unique_common.js")));
            }

            [Fact]
            public void Will_copy_files_referenced_from_test_file_to_temporary_folder_if_they_already_exists()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\lib.js")).Returns(true);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\temp\common.js")).Returns(true);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
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

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\unique_lib.js", true));
                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\common.js", @"C:\temp\unique_common.js", true));
            }

            [Fact]
            public void Will_put_test_js_file_at_end_of_references_in_html_template_with_test_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
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

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"common.js");
                string scriptStatement3 = TestContextBuilder.GetScriptStatement(@"test.js");
                var pos1 = text.IndexOf(scriptStatement1);
                var pos2 = text.IndexOf(scriptStatement2);
                var pos3 = text.IndexOf(scriptStatement3);
                Assert.True(pos1 < pos2);
                Assert.True(pos2 < pos3);
                Assert.Equal(1, context.ReferencedJavaScriptFiles.Count(x => x.IsFileUnderTest));
            }

            [Fact]
            public void Will_replace_referenced_file_place_holder_in_html_template_with_referenced_files_from_js_test_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestJSFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathType("test.js")).Returns(PathType.JavaScript);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"common.js");
                Assert.Contains(scriptStatement1, text);
                Assert.Contains(scriptStatement2, text);
                Assert.DoesNotContain("@@ReferencedFiles@@", text);
            }

            [Fact]
            public void Will_replace_referenced_file_place_holder_in_html_template_with_referenced_files_from_html_test_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("testFile.html")).Returns(@"path\testFile.html");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\testFile.html"))
                    .Returns(TestHTMLFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathType("testFile.html")).Returns(PathType.Html);

                var context = creator.ClassUnderTest.BuildContext("testFile.html");

                string scriptStatement1 = TestContextBuilder.GetScriptStatement(@"lib.js");
                string scriptStatement2 = TestContextBuilder.GetScriptStatement(@"common.js");
                Assert.Contains(scriptStatement1, text);
                Assert.Contains(scriptStatement2, text);
                Assert.DoesNotContain("@@ReferencedFiles@@", text);
            }

            [Fact]
            public void Will_not_copy_referenced_path_if_not_a_file()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
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
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"path\test.js");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                var context = creator.ClassUnderTest.BuildContext("test.js");

                string scriptStatement = TestContextBuilder.GetScriptStatement(@"http://a.com/lib.js");
                Assert.Contains(scriptStatement, text);
            }




            [Fact]
            public void Will_replace_text_fixture_place_holder_with_existing_fixture()
            {
                TestableHtmlTestFileCreator creator = new TestableHtmlTestFileCreator();
                string text = null;
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetTemporaryFolder()).Returns(@"C:\temp\");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("testFile.html")).Returns(@"path\testFile.html");
                creator.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\testFile.html"))
                    .Returns(TestHTMLFileContents);
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");
                creator.Mock<IFileProbe>().Setup(x => x.GetPathType("testFile.html")).Returns(PathType.Html);

                var context = creator.ClassUnderTest.BuildContext("testFile.html");

                Assert.Contains("<div> some <a>fixture</a> content </div>", text);
                Assert.DoesNotContain("@@FixtureContent@@", text);
            }

        }
    }
}