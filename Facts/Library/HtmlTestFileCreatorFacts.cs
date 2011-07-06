using System;
using System.IO;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class HtmlTestFileCreatorFacts
    {
        private class TestableHtmlTestFileCreator : HtmlTestFileCreator
        {
            public Mock<IFileSystemWrapper> MoqFileSystemWrapper { get; set; }
            public Mock<IFileProbe> MoqFileProbe { get; set; }

            public TestableHtmlTestFileCreator(Mock<IFileSystemWrapper> moqFileSystemWrapper, Mock<IFileProbe> moqFileProbe)
                : base(moqFileSystemWrapper.Object, moqFileProbe.Object)
            {
                MoqFileSystemWrapper = moqFileSystemWrapper;
                MoqFileProbe = moqFileProbe;
            }

            public static TestableHtmlTestFileCreator Create()
            {
                var creator = new TestableHtmlTestFileCreator(new Mock<IFileSystemWrapper>(), new Mock<IFileProbe>());

                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                creator.MoqFileProbe.Setup(x => x.FindPath(It.IsAny<string>())).Returns<string>(x => x);
                creator.MoqFileSystemWrapper.Setup(x => x.GetText(It.IsAny<string>())).Returns("");
                creator.MoqFileSystemWrapper.Setup(x => x.GetRandomFileName()).Returns("unique");
                creator.MoqFileSystemWrapper.Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                return creator;
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
            get { return EmbeddedManifestResourceReader.GetEmbeddedResoureText<TestRunner>("Chutzpah.TestFiles.testTemplate"); }
        }

        public class CreateTestFile_WithPath
        {
            [Fact]
            public void Will_create_test_folder_if_does_not_exists()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileSystemWrapper.Setup(x => x.FolderExists(@"C:\existing")).Returns(false);

                var res = creator.CreateTestFile("test.js", @"C:\existing");

                creator.MoqFileSystemWrapper.Verify(x => x.CreateDirectory(@"C:\existing"));
            }
        }

        public class CreateTestFile
        {
            [Fact]
            public void Will_throw_if_test_file_is_null()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();

                Exception ex = Record.Exception(() => creator.CreateTestFile(null));

                Assert.IsType<ArgumentNullException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_is_not_a_js_file()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();

                Exception ex = Record.Exception(() => creator.CreateTestFile("test.blah"));

                Assert.IsType<ArgumentException>(ex);
            }

            [Fact]
            public void Will_throw_if_test_file_does_not_exist()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns((string)null);

                Exception ex = Record.Exception(() => creator.CreateTestFile("test.js"));

                Assert.IsType<FileNotFoundException>(ex);
            }

            [Fact]
            public void Will_save_generated_test_html_file_to_temporary_folder_and_return_path()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()));
                Assert.Equal(@"C:\temp\test.html", path);
            }

            [Fact]
            public void Will_save_qunit_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.Save(@"C:\temp\qunit.js", QUnitContents));
            }

            [Fact]
            public void Will_save_qunit_css_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.Save(@"C:\temp\qunit.css", QUnitCssContents));
            }

            [Fact]
            public void Will_copy_test_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns(@"path\test.js");
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                creator.MoqFileSystemWrapper.Setup(x => x.GetText(@"path\test.js")).Returns("contents");

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.Save(@"C:\temp\test.js", "contents"));
            }

            [Fact]
            public void Will_replace_test_file_place_holder_in_html_template_with_test_file()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                string text = null;
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                creator.MoqFileSystemWrapper
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);

                string path = creator.CreateTestFile(@"some\path\test.js");

                string scriptStatement = HtmlTestFileCreator.GetScriptStatement(@"test.js");
                Assert.Contains(scriptStatement, text);
                Assert.DoesNotContain("@@TestFiles@@", text);
            }

            const string TestFileContents =
    @"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/common.js"" />
                        some javascript code
                        ";


            [Fact]
            public void Will_copy_files_referenced_from_test_file_to_temporary_folder()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns(@"path\test.js");
                creator.MoqFileSystemWrapper
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);
                creator.MoqFileProbe
                    .Setup(x => x.FindPath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.MoqFileProbe
                    .Setup(x => x.FindPath(Path.Combine(@"path\", @"../../js/common.js")))
                   .Returns(@"path\common.js");

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\lib.js", true));
                creator.MoqFileSystemWrapper.Verify(x => x.CopyFile(@"path\common.js", @"C:\temp\common.js", true));
            }

            [Fact]
            public void Will_copy_files_referenced_from_test_file_to_temporary_folder_if_they_already_exists()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                creator.MoqFileSystemWrapper.Setup(x => x.FileExists(@"C:\temp\lib.js")).Returns(true);
                creator.MoqFileSystemWrapper.Setup(x => x.FileExists(@"C:\temp\common.js")).Returns(true);
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns(@"path\test.js");
                creator.MoqFileSystemWrapper
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);
                creator.MoqFileProbe
                    .Setup(x => x.FindPath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.MoqFileProbe
                    .Setup(x => x.FindPath(Path.Combine(@"path\", @"../../js/common.js")))
                   .Returns(@"path\common.js");

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.CopyFile(@"path\lib.js", @"C:\temp\unique_lib.js", true));
                creator.MoqFileSystemWrapper.Verify(x => x.CopyFile(@"path\common.js", @"C:\temp\unique_common.js", true));
            }

            [Fact]
            public void Will_replace_referenced_file_place_holder_in_html_template_with_referenced_files()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                string text = null;
                creator.MoqFileSystemWrapper
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns(@"path\test.js");
                creator.MoqFileSystemWrapper
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);
                creator.MoqFileProbe
                    .Setup(x => x.FindPath(Path.Combine(@"path\", @"lib.js")))
                    .Returns(@"path\lib.js");
                creator.MoqFileProbe
                    .Setup(x => x.FindPath(Path.Combine(@"path\", @"../../js/common.js")))
                    .Returns(@"path\common.js");

                string path = creator.CreateTestFile("test.js");

                string scriptStatement1 = HtmlTestFileCreator.GetScriptStatement(@"lib.js");
                string scriptStatement2 = HtmlTestFileCreator.GetScriptStatement(@"common.js");
                Assert.Contains(scriptStatement1, text);
                Assert.Contains(scriptStatement2, text);
                Assert.DoesNotContain("@@ReferencedFiles@@", text);
            }

            [Fact]
            public void Will_not_copy_referenced_path_is_not_a_file()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns(@"path\test.js");
                creator.MoqFileSystemWrapper
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                string path = creator.CreateTestFile("test.js");

                creator.MoqFileSystemWrapper.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Never());
            }

            [Fact]
            public void Will_replace_referenced_file_place_holder_with_referenced_uri()
            {
                TestableHtmlTestFileCreator creator = TestableHtmlTestFileCreator.Create();
                string text = null;
                creator.MoqFileSystemWrapper
                    .Setup(x => x.Save(@"C:\temp\test.html", It.IsAny<string>()))
                    .Callback<string, string>((x, y) => text = y);
                creator.MoqFileSystemWrapper.Setup(x => x.GetTemporayFolder()).Returns(@"C:\temp\");
                string TestFileContents = @"/// <reference path=""http://a.com/lib.js"" />";
                creator.MoqFileProbe.Setup(x => x.FindPath("test.js")).Returns(@"path\test.js");
                creator.MoqFileSystemWrapper
                    .Setup(x => x.GetText(@"path\test.js"))
                    .Returns(TestFileContents);

                string path = creator.CreateTestFile("test.js");

                string scriptStatement = HtmlTestFileCreator.GetScriptStatement(@"http://a.com/lib.js");
                Assert.Contains(scriptStatement, text);
            }

        }
    }
}