using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class FileProbeFacts
    {
        private class TestableFileProbe : Testable<FileProbe>
        {
            public TestableFileProbe()
            {
                Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns("someDirName");
            }
        }

        public class FindFilePath
        {
            [Fact]
            public void Will_return_null_if_fileName_is_null_or_empty()
            {
                var probe = new TestableFileProbe();

                var path = probe.ClassUnderTest.FindFilePath(null);

                Assert.Null(path);
            }

            [Fact]
            public void Will_return_full_path_of_fileName_in_executing_if_file_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IEnvironmentWrapper>().Setup(x => x.GetExeuctingAssemblyPath()).Returns(@"c:\dir\thing.exe");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"c:\dir\path.html")).Returns(true);
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(@"c:\dir\thing.exe")).Returns(@"c:\dir");

                var path = prob.ClassUnderTest.FindFilePath("path.html");

                Assert.Equal(@"c:\dir\path.html", path);
            }

            [Fact]
            public void Will_return_full_path_of_fileName_in_current_directory_if_file_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(@"path.html")).Returns(@"d:\other\path.html");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"d:\other\path.html")).Returns(true);


                var path = prob.ClassUnderTest.FindFilePath("path.html");

                Assert.Equal(@"d:\other\path.html", path);
            }

            [Fact]
            public void Will_return_null_if_all_attempts_fail()
            {
                var prob = new TestableFileProbe();

                var path = prob.ClassUnderTest.FindFilePath("somePath");

                Assert.Null(path);
            }
        }

        public class FindFolderPath
        {
            [Fact]
            public void Will_return_null_if_folderName_is_null_or_empty()
            {
                var probe = new TestableFileProbe();

                var path = probe.ClassUnderTest.FindFolderPath(null);

                Assert.Null(path);
            }

            [Fact]
            public void Will_return_full_path_of_folderName_in_executing_if_folder_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IEnvironmentWrapper>().Setup(x => x.GetExeuctingAssemblyPath()).Returns(@"c:\dir\thing.exe");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(@"c:\dir\path")).Returns(true);
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(@"c:\dir\thing.exe")).Returns(@"c:\dir");

                var path = prob.ClassUnderTest.FindFolderPath("path");

                Assert.Equal(@"c:\dir\path", path);
            }

            [Fact]
            public void Will_return_full_path_of_folderName_in_current_directory_if_folder_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(@"path")).Returns(@"d:\other\path");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(@"d:\other\path")).Returns(true);


                var path = prob.ClassUnderTest.FindFolderPath("path");

                Assert.Equal(@"d:\other\path", path);
            }

            [Fact]
            public void Will_return_null_if_all_attempts_fail()
            {
                var prob = new TestableFileProbe();

                var path = prob.ClassUnderTest.FindFolderPath("somePath");

                Assert.Null(path);
            }
        }

        public class GetPathType
        {
            [Fact]
            public void Will_return_folder_type_for_folder()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(@"C:\someFolder")).Returns(true);

                var type = probe.ClassUnderTest.GetPathType(@"C:\someFolder");

                Assert.Equal(PathType.Folder, type);
            }

            [Fact]
            public void Will_return_html_type_for_html_file()
            {
                var probe = new TestableFileProbe();

                var type = probe.ClassUnderTest.GetPathType(@"C:\someFolder\a.html");

                Assert.Equal(PathType.Html, type);
            }

            [Fact]
            public void Will_return_JavaScript_type_for_js_file()
            {
                var probe = new TestableFileProbe();

                var type = probe.ClassUnderTest.GetPathType(@"C:\someFolder\a.js");

                Assert.Equal(PathType.JavaScript, type);
            }

            [Fact]
            public void Will_return_Other_type_for_anything_else()
            {
                var probe = new TestableFileProbe();

                var type = probe.ClassUnderTest.GetPathType(@"C:\someFolder\a.blah");

                Assert.Equal(PathType.Other, type);
            }
        }

        public class FindTestableFiles
        {
            [Fact]
            public void Will_return_empty_list_if_paths_is_null()
            {
                var probe = new TestableFileProbe();

                var res = probe.ClassUnderTest.FindScriptFiles(null);

                Assert.Empty(res);
            }

            [Fact]
            public void Will_return_files_that_are_html_or_js()
            {
                var probe = new TestableFileProbe();
                var paths = new List<string>
                            {
                                "a.js",
                                "b.html",
                                "c.blah"
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths);

                Assert.Equal(2, res.Count());
                Assert.Contains("a.js", res);
                Assert.Contains("b.html", res);
            }

            [Fact]
            public void Will_return_js_files_that_are_found_in_given_folder()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns("");
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists("folder")).Returns(true);
                probe.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles("folder", "*.js", SearchOption.AllDirectories))
                    .Returns(new string[] { "subFile1.js", "subFile2.js" });
                var paths = new List<string>
                            {
                                "folder"
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths);

                Assert.Equal(2, res.Count());
                Assert.Contains("subFile1.js", res);
            }

        }
    }
}