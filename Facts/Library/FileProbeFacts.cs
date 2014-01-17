using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
                Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(It.IsAny<string>())).Returns<string>(x => x);
            }
        }

        public class FindTestSettingsFile
        {
            [Fact]
            public void Will_find_settings_file_in_current_directory()
            {
                var probe = new TestableFileProbe();
                var dir = @"C:\a\b\c";
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\a\b\c\" + Constants.SettingsFileName)).Returns(true);

                var file = probe.ClassUnderTest.FindTestSettingsFile(dir);

                Assert.NotNull(file);
            }

            [Fact]
            public void Will_find_settings_file_in_by_traversing_up_tree()
            {
                var probe = new TestableFileProbe();
                var dir = @"C:\a\b\c";
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\" + Constants.SettingsFileName)).Returns(true);

                var file = probe.ClassUnderTest.FindTestSettingsFile(dir);

                Assert.NotNull(file);
            }

            [Fact]
            public void Will_return_null_when_file_not_found()
            {
                var probe = new TestableFileProbe();
                var dir = @"C:\a\b\c";
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

                var file = probe.ClassUnderTest.FindTestSettingsFile(dir);

                Assert.Null(file);
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
            public void Will_return_path_if_web_url()
            {
                var prob = new TestableFileProbe();

                var path = prob.ClassUnderTest.FindFilePath("http://someurl.com");

                Assert.Equal("http://someurl.com", path);
            }

            [Fact]
            public void Will_return_full_path_of_fileName_in_executing_if_file_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IEnvironmentWrapper>().Setup(x => x.GetExeuctingAssemblyPath()).Returns(@"c:\dir\thing.exe");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"c:\dir\full\path.html")).Returns(true);
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(@"c:\dir\thing.exe")).Returns(@"c:\dir");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(@"c:\dir\path.html")).Returns(@"c:\dir\full\path.html");

                var path = prob.ClassUnderTest.FindFilePath("path.html");

                Assert.Equal(@"c:\dir\full\path.html", path);
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
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(@"c:\dir\full\path")).Returns(true);
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(@"c:\dir\thing.exe")).Returns(@"c:\dir");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(@"c:\dir\path")).Returns(@"c:\dir\full\path");

                var path = prob.ClassUnderTest.FindFolderPath("path");

                Assert.Equal(@"c:\dir\full\path", path);
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

        public class GetPathInfo
        {
            [Fact]
            public void Will_return_folder_type_for_folder()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(@"C:\someFolder")).Returns(true);

                var info = probe.ClassUnderTest.GetPathInfo(@"C:\someFolder");

                Assert.Equal(PathType.Folder, info.Type);
                Assert.Equal(@"C:\someFolder", info.FullPath);
            }

            [Fact]
            public void Will_put_input_path_into_path_property()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"fullPath")).Returns(true);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath("shortPath")).Returns("fullPath");
                var info = probe.ClassUnderTest.GetPathInfo(@"shortPath");

                Assert.Equal(@"fullPath", info.FullPath);
                Assert.Equal(@"shortPath", info.Path);
            }


            [Fact]
            public void Will_return_url_type_for_url_file()
            {
                var probe = new TestableFileProbe();

                var info = probe.ClassUnderTest.GetPathInfo(@"http://url.com/site");

                Assert.Equal(PathType.Url, info.Type);
                Assert.Equal(@"http://url.com/site", info.FullPath);
            }

            [Fact]
            public void Will_return_html_type_for_html_file()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\someFolder\a.html")).Returns(true);

                var info = probe.ClassUnderTest.GetPathInfo(@"C:\someFolder\a.html");

                Assert.Equal(PathType.Html, info.Type);
                Assert.Equal(@"C:\someFolder\a.html", info.FullPath);
            }

            [Fact]
            public void Will_return_JavaScript_type_for_js_file()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\someFolder\a.js")).Returns(true);

                var info = probe.ClassUnderTest.GetPathInfo(@"C:\someFolder\a.js");

                Assert.Equal(PathType.JavaScript, info.Type);
                Assert.Equal(@"C:\someFolder\a.js", info.FullPath);
            }


            [Fact]
            public void Will_return_CoffeeScript_type_for_coffee_file()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\someFolder\a.coffee")).Returns(true);

                var info = probe.ClassUnderTest.GetPathInfo(@"C:\someFolder\a.coffee");

                Assert.Equal(PathType.CoffeeScript, info.Type);
                Assert.Equal(@"C:\someFolder\a.coffee", info.FullPath);
            }


            [Fact]
            public void Will_return_TypeScript_type_for_typescript_file()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"C:\someFolder\a.ts")).Returns(true);

                var info = probe.ClassUnderTest.GetPathInfo(@"C:\someFolder\a.ts");

                Assert.Equal(PathType.TypeScript, info.Type);
                Assert.Equal(@"C:\someFolder\a.ts", info.FullPath);
            }

            [Fact]
            public void Will_return_Other_type_for_anything_else()
            {
                var probe = new TestableFileProbe();

                var info = probe.ClassUnderTest.GetPathInfo(@"C:\someFolder\a.blah");

                Assert.Equal(PathType.Other, info.Type);
                Assert.Null(info.FullPath);
            }
        }

        public class IsChutzpahTemporaryFile
        {
            [Fact]
            public void Will_return_false_if_path_is_empty()
            {
                var probe = new TestableFileProbe();

                var res = probe.ClassUnderTest.IsTemporaryChutzpahFile(null);

                Assert.False(res);
            }

            [Fact]
            public void Will_return_false_if_path_does_not_start_with_chutzpah_temp_prefix()
            {
                var probe = new TestableFileProbe();

                var res = probe.ClassUnderTest.IsTemporaryChutzpahFile("path\\" + "a.js");

                Assert.False(res);
            }

            [Fact]
            public void Will_return_true_if_path_does_starts_with_chutzpah_temp_prefix()
            {
                var probe = new TestableFileProbe();

                var res = probe.ClassUnderTest.IsTemporaryChutzpahFile("path\\" + string.Format(Constants.ChutzpahTemporaryFileFormat, Thread.CurrentThread.ManagedThreadId, "a.js"));

                Assert.True(res);
            }
        }

        public class FindScriptFiles_Paths
        {
            [Fact]
            public void Will_return_empty_list_if_paths_is_null()
            {
                var probe = new TestableFileProbe();

                var res = probe.ClassUnderTest.FindScriptFiles((IEnumerable<string>)null, TestingMode.All);

                Assert.Empty(res);
            }

            [Fact]
            public void Will_return_valid_test_files_when_testing_mode_is_all()
            {
                var probe = new TestableFileProbe();

                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(It.IsAny<string>())).Returns<string>(x => @"somePath\" + x);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                var paths = new List<string>
                            {
                                "a.js",
                                "b.html",
                                "c.blah",
                                "d.htm",
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths, TestingMode.All);

                Assert.Equal(3, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains(@"somePath\a.js", fullPaths);
                Assert.Contains(@"somePath\b.html", fullPaths);
                Assert.Contains(@"somePath\d.htm", fullPaths);
            }

            [Fact]
            public void Will_return_files_that_match_testing_mode()
            {
                var probe = new TestableFileProbe();

                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(It.IsAny<string>())).Returns<string>(x => @"somePath\" + x);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                var paths = new List<string>
                            {
                                "a.js",
                                "a.coffee",
                                "b.html",
                                "c.blah",
                                "d.htm",
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths, TestingMode.JavaScript);

                Assert.Equal(1, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains(@"somePath\a.js", fullPaths);
            }

            [Fact]
            public void Will_return_files_that_are_html_when_testing_mode_is_HTML()
            {
                var probe = new TestableFileProbe();

                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(It.IsAny<string>())).Returns<string>(x => @"somePath\" + x);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                var paths = new List<string>
                            {
                                "a.js",
                                "a.coffee",
                                "b.html",
                                "c.blah",
                                "d.htm",
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths, TestingMode.HTML);

                Assert.Equal(2, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains(@"somePath\b.html", fullPaths);
                Assert.Contains(@"somePath\d.htm", fullPaths);
            }

            [Fact]
            public void Will_return_urls_when_testing_mode_is_HTML()
            {
                var probe = new TestableFileProbe();
                var paths = new List<string> { "http://someurl.com/path" };

                var res = probe.ClassUnderTest.FindScriptFiles(paths, TestingMode.HTML);

                Assert.Equal(1, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains("http://someurl.com/path", fullPaths);
            }

            [Fact]
            public void Will_return_js_or_coffee_or_typescript_files_that_are_found_in_given_folder()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns("");
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists("folder")).Returns(true);
                probe.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles("folder", "*.*", SearchOption.AllDirectories))
                    .Returns(new string[] { "subFile1.js", "subFile2.coffee", "subFile3.ts", "subFile4.html" });
                var paths = new List<string>
                            {
                                "folder"
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths, TestingMode.AllExceptHTML);

                Assert.Equal(3, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains("subFile1.js", fullPaths);
                Assert.Contains("subFile2.coffee", fullPaths);
                Assert.Contains("subFile3.ts", fullPaths);
            }

            [Fact]
            public void Will_skip_chutzpah_temporary_files_found_in_folders()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns("");
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists("folder")).Returns(true);
                probe.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles("folder", "*.*", SearchOption.AllDirectories))
                    .Returns(new string[] { "subFile1.js", string.Format(Constants.ChutzpahTemporaryFileFormat, Thread.CurrentThread.ManagedThreadId, "subFile2.js"), });
                var paths = new List<string>
                            {
                                "folder"
                            };

                var res = probe.ClassUnderTest.FindScriptFiles(paths, TestingMode.AllExceptHTML);

                Assert.Equal(1, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains("subFile1.js", fullPaths);
            }

        }


        public class FindScriptFiles_SettingsFile
        {
            [Fact]
            public void Will_return_empty_list_if_paths_is_null()
            {
                var probe = new TestableFileProbe();

                var res = probe.ClassUnderTest.FindScriptFiles((ChutzpahTestSettingsFile)null);

                Assert.Empty(res);
            }


            [Fact]
            public void Will_return_explicity_path_from_tests_setting_if_exists()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(It.IsAny<string>())).Returns<string>(x => x);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                var setting = new ChutzpahTestSettingsFile
                {
                    SettingsFileDirectory = "dir",
                    Tests = new List<SettingsFileTestPath>
                    {
                        new SettingsFileTestPath
                        {
                            Path = "file.js"
                        }
                    }
                };
                var res = probe.ClassUnderTest.FindScriptFiles(setting);

                Assert.Equal(1, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains(@"dir\file.js", fullPaths);
            }


            [Fact]
            public void Will_return_paths_from_folder_which_match_include_exclude_patterns()
            {
                var probe = new TestableFileProbe();
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(It.IsAny<string>())).Returns<string>(x => x);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                probe.Mock<IFileSystemWrapper>().Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                    .Returns(new[]
                    {
                        "somefolder/src/a.js",
                        "somefolder/src/B.JS",
                        "somefolder/src/c.ts",
                        "somefolder\\src\\d.ts",
                        "somefolder/e.ts"
                    });
                var setting = new ChutzpahTestSettingsFile
                {
                    SettingsFileDirectory = "dir",
                    Tests = new List<SettingsFileTestPath>
                    {
                        new SettingsFileTestPath
                        {
                            Path = "someFolder",
                            Include = "*src/*",
                            Exclude = "*.js",
                        }
                    }
                };
                var res = probe.ClassUnderTest.FindScriptFiles(setting);

                Assert.Equal(2, res.Count());
                var fullPaths = res.Select(x => x.FullPath);
                Assert.Contains(@"somefolder/src/c.ts", fullPaths);
                Assert.Contains(@"somefolder\src\d.ts", fullPaths);
            }


        }
    }
}