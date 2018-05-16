using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            public List<ReferencedFile> ReferenceFiles { get; set; }
            public ChutzpahTestSettingsFile ChutzpahTestSettingsFile { get; set; }
            public TestableTestContextBuilder()
            {
                ReferenceFiles = new List<ReferencedFile>();
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
                Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                Mock<IFileProbe>()
                .Setup(x => x.BuiltInDependencyDirectory)
                .Returns(@"dependencyPath\");
                ChutzpahTestSettingsFile = new ChutzpahTestSettingsFile
                {
                    SettingsFileDirectory = "C:\\settingsPath"
                }.InheritFromDefault();
                Mock<IChutzpahTestSettingsService>().Setup(x => x.FindSettingsFile(It.IsAny<string>(), It.IsAny<ChutzpahSettingsFileEnvironments>())).Returns(ChutzpahTestSettingsFile);


                Mock<IReferenceProcessor>()
                    .Setup(x => x.GetReferencedFiles(
                                It.IsAny<List<ReferencedFile>>(),
                                It.IsAny<IFrameworkDefinition>(),
                                It.IsAny<ChutzpahTestSettingsFile>()))
                    .Callback<List<ReferencedFile>, IFrameworkDefinition, ChutzpahTestSettingsFile>(
                        (refs, def, settings) =>
                        {
                            refs.AddRange(ReferenceFiles);
                        });
            }
        }

        public class IsTestFile
        {
            [Fact]
            public void Will_return_false_if_test_file_is_null()
            {
                var creator = new TestableTestContextBuilder();

                var result = creator.ClassUnderTest.IsTestFile(null, null);

                Assert.False(result);
            }

            [Theory]
            [InlineData("test.ts", PathType.JavaScript)]
            [InlineData("test.coffee", PathType.JavaScript)]
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
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns((string)null);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_test_framework_not_detected()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns("test.js");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(false);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_true_if_settings_include_path_matches()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.SettingsFileDirectory = @"C:\settingsPath";
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Include = "*test.js", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\settingspath")).Returns(@"c:\settingspath");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns( @"C:\settingsPath\path\test.js" );
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.True(result);
            }

            [Fact]
            public void Will_return_false_if_settings_include_path_matches_but_exclude_doesnt()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.SettingsFileDirectory = @"C:\settingsPath";
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\settingspath")).Returns(@"c:\settingspath");
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Include = "*test.js", Exclude = "*path/test.js"} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"C:\settingsPath\path\test.js");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_true_if_settings_exclude_doesnt_match()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.SettingsFileDirectory = @"C:\settingsPath";
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\settingspath")).Returns(@"c:\settingspath");
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Exclude = "*path2/test.js", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("test.js")).Returns(@"C:\settingsPathpath\test.js");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile("test.js");

                Assert.True(result);
            }

            [Fact]
            public void Will_return_true_if_settings_path_matches()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Path = "path/test.js", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("c:\test.js")).Returns(@"c:\settingsPath\path\test.js");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile("c:\test.js");

                Assert.True(result);
            }

            [Fact]
            public void Will_return_true_if_folder_path_matches_with_no_includeExcludes()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Path = "path/", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\test.js")).Returns(@"c:\settingsPath\path\test.js");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("C:\\settingsPath\\path\\")).Returns((string)null);
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath("C:\\settingsPath\\path\\")).Returns(@"c:\settingsPath\path");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile(@"c:\test.js");

                Assert.True(result);
            }

            [Fact]
            public void Will_return_true_if_folder_path_matches_with_include()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Path = "path/", Include = "*.js", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\test.js")).Returns(@"c:\settingsPath\path\test.js");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\settingsPath\path\")).Returns((string)null);
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsPath\path\")).Returns(@"c:\settingsPath\path");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile(@"c:\test.js");

                Assert.True(result);
            }

            [Fact]
            public void Will_return_false_if_folder_path_matches_but_include_does_not_match()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Path = "path/", Include = "*.ts", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\test.js")).Returns(@"c:\settingsPath\path\test.js");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("settingsPath\\path\\")).Returns((string)null);
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath("settingsPath\\path\\")).Returns(@"c:\settingsPath\path");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile(@"c:\test.js");

                Assert.False(result);
            }

            [Fact]
            public void Will_return_false_if_folder_path_matches_but_exclude_match()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.Tests = new[]
                {
                    new SettingsFileTestPath{ Path = "path/", Exclude = "*.js", SettingsFileDirectory = creator.ChutzpahTestSettingsFile.SettingsFileDirectory} 
                };
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\test.js")).Returns(@"settingsPath\path\test.js");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath("settingsPath\\path\\")).Returns((string)null);
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath("settingsPath\\path\\")).Returns(@"settingsPath\path");
                creator.Mock<IFrameworkDefinition>().Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);

                var result = creator.ClassUnderTest.IsTestFile(@"c:\test.js");

                Assert.False(result);
            }



            [Fact]
            public void Will_set_harness_folder_when_test_file_adjacent()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\folder\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.Equal(@"C:\folder", context.TestHarnessDirectory);
                Assert.Equal(@"C:\folder\test.js", context.InputTestFiles.FirstOrDefault());
            }

            [Fact]
            public void Will_set_harness_folder_when_settings_file_adjacent()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.TestHarnessLocationMode = TestHarnessLocationMode.SettingsFileAdjacent;
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("c:\test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\folder1\test.js" });

                var context = creator.ClassUnderTest.BuildContext("c:\test.js", new TestOptions());

                Assert.Equal(@"C:\settingsPath", context.TestHarnessDirectory);
            }

            [Fact]
            public void Will_set_harness_folder_when_custom_placement()
            {
                var creator = new TestableTestContextBuilder();
                creator.ChutzpahTestSettingsFile.TestHarnessLocationMode = TestHarnessLocationMode.Custom;
                creator.ChutzpahTestSettingsFile.TestHarnessDirectory = "customFolder";
                creator.Mock<IFileProbe>().Setup(x => x.FindFolderPath(It.Is<string>(p => p.Contains("customFolder")))).Returns("customFolder");
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("test.js"))
                    .Returns(new PathInfo { Type = PathType.JavaScript, FullPath = @"C:\folder3\test.js" });

                var context = creator.ClassUnderTest.BuildContext("test.js", new TestOptions());

                Assert.Equal(@"customFolder", context.TestHarnessDirectory);
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
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test.blah")).Returns(new PathInfo { FullPath = "somePath", Type = PathType.Other });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext("test.blah", new TestOptions()));

                Assert.IsType<ArgumentException>(ex);
            }

            [Fact]
            public void Will_throw_if_more_than_one_html_type_is_given()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test1.html")).Returns(new PathInfo { FullPath = "somePath", Type = PathType.Html });
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test2.html")).Returns(new PathInfo { FullPath = "somePath", Type = PathType.Html });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext(new []{"test1.html", "test2.html"}, new TestOptions()));

                Assert.IsType<InvalidOperationException>(ex);
            }

            [Fact]
            public void Will_throw_if_more_than_one_url_type_is_given()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test1.html")).Returns(new PathInfo { FullPath = "somePath", Type = PathType.Url });
                creator.Mock<IFileProbe>().Setup(x => x.GetPathInfo("test2.html")).Returns(new PathInfo { FullPath = "somePath", Type = PathType.Url });

                Exception ex = Record.Exception(() => creator.ClassUnderTest.BuildContext(new[] { "test1.html", "test2.html" }, new TestOptions()));

                Assert.IsType<InvalidOperationException>(ex);
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
            public void Will_return_path_and_framework_for_html_file()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>()
                    .Setup(x => x.GetPathInfo("testThing.html"))
                    .Returns(new PathInfo { Type = PathType.Html, FullPath = @"C:\testThing.html" });

                var context = creator.ClassUnderTest.BuildContext("testThing.html", new TestOptions());

                Assert.Equal(@"qunitRunner.js", context.TestRunner);
                Assert.Equal(@"C:\testThing.html", context.TestHarnessPath);
                Assert.Equal(@"C:\testThing.html", context.InputTestFiles.FirstOrDefault());
                Assert.False(context.IsRemoteHarness);
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
                Assert.Equal(@"http://someUrl.com", context.InputTestFiles.FirstOrDefault());
                Assert.True(context.IsRemoteHarness);
            }

            [Fact]
            public void Will_set_js_test_file_to_file_under_test()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\test.js")).Returns(@"C:\path\test.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"C:\path\test.js")).Returns("contents");

                var context = creator.ClassUnderTest.BuildContext(@"C:\test.js", new TestOptions());

                Assert.True(context.ReferencedFiles.SingleOrDefault(x => x.Path.Contains("test.js")).IsFileUnderTest);
            }

            [Fact]
            public void Will_not_copy_referenced_file_if_it_is_the_test_runner()
            {
                var creator = new TestableTestContextBuilder();
                creator.ReferenceFiles.Add(new ReferencedFile { Path = @"path\qunit.js" });

                var context = creator.ClassUnderTest.BuildContext(@"c:\test.js", new TestOptions());

                creator.Mock<IFileSystemWrapper>().Verify(x => x.CopyFile(@"path\qunit.js", @"C:\temp\qunit.js", true), Times.Never());
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

            [Fact]
            public void Will_set_multiple_js_test_files_to_file_under_test()
            {
                var creator = new TestableTestContextBuilder();
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\test1.js")).Returns(@"C:\path\test1.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"C:\path\test1.js")).Returns("contents1");
                creator.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\test2.js")).Returns(@"C:\path\test2.js");
                creator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"C:\path\test2.js")).Returns("contents2");

                var context = creator.ClassUnderTest.BuildContext(new []{@"C:\test1.js",@"C:\test2.js"}, new TestOptions());

                Assert.True(context.ReferencedFiles.SingleOrDefault(x => x.Path.Contains("test1.js")).IsFileUnderTest);
                Assert.True(context.ReferencedFiles.SingleOrDefault(x => x.Path.Contains("test2.js")).IsFileUnderTest);
            }
        }
    }
}