using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class ReferenceProcessorFacts
    {
        private class TestableReferenceProcessor : Testable<ReferenceProcessor>
        {
            public IFrameworkDefinition FrameworkDefinition { get; set; }

            public TestableReferenceProcessor()
            {
                var frameworkMock = Mock<IFrameworkDefinition>();
                frameworkMock.Setup(x => x.FileUsesFramework(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<PathType>())).Returns(true);
                frameworkMock.Setup(x => x.FrameworkKey).Returns("qunit");
                frameworkMock.Setup(x => x.GetTestRunner(It.IsAny<ChutzpahTestSettingsFile>())).Returns("qunitRunner.js");
                frameworkMock.Setup(x => x.GetTestHarness(It.IsAny<ChutzpahTestSettingsFile>())).Returns("qunit.html");
                frameworkMock.Setup(x => x.GetFileDependencies(It.IsAny<ChutzpahTestSettingsFile>())).Returns(new[] { "qunit.js", "qunit.css" });
                FrameworkDefinition = frameworkMock.Object;

                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x => x);
                Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns(string.Empty);
            }

        }

        public class SetupAmdFilePaths
        {
            [Fact]
            public void Will_set_amd_path_for_reference_path()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\path";
                var referencedFile = new ReferencedFile {Path = @"C:\some\path\code\test.js"};
                var referenceFiles = new List<ReferencedFile> { referencedFile };
    
                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles,testHarnessDirectory, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal("code/test",referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }

            [Fact]
            public void Will_make_amd_path_relative_to_amdbaseurl_if_no_amdappdirectory_if_given()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\path";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile { AMDBaseUrl = @"C:\some\other" };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../path/code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }

            [Fact]
            public void Will_make_amd_path_relative_to_amdappdirectory_if_given()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\path";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile { AMDAppDirectory = @"C:\some\other" };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../path/code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }


            [Fact]
            public void Will_not_replace_directory_name_containing_extension_in_relative_amd_path()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\src\folder";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path.jstests\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile { };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../../path.jstests/code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }


            [Fact]
            public void Will_not_replace_directory_name_containing_extension_in_relative_amd_path_with_legacy_setting()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\src\folder";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path.jstests\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile { AMDBasePath = @"C:\some\other" };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../src/folder/../../path.jstests/code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }


            [Fact]
            public void Will_make_amd_path_relative_to_amdbasepath_with_legacy_setting()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\path";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile {AMDBasePath = @"C:\some\other"};

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../path/code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }


            [Fact]
            public void Will_make_amd_path_relative_to_testHarnessLocation()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\src\folder";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile { };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../../path/code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }


            [Fact]
            public void Will_make_amd_path_relative_to_testHarnessLocation_and_amdbasepath()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"c:\some\path\subFolder";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };
                var settings = new ChutzpahTestSettingsFile { AMDBasePath = @"C:\some\other" };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, settings);

                Assert.Equal("../path/subFolder/../code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }


            [Fact]
            public void Will_set_amd_path_ignoring_the_case()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"C:\Some\Path";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal("code/test", referencedFile.AmdFilePath);
                Assert.Null(referencedFile.AmdGeneratedFilePath);
            }

            [Fact]
            public void Will_set_amd_path_for_reference_path_and_generated_path()
            {
                var processor = new TestableReferenceProcessor();
                var testHarnessDirectory = @"C:\some\path";
                var referencedFile = new ReferencedFile { Path = @"C:\some\path\code\test.ts", GeneratedFilePath = @"C:\some\path\code\_Chutzpah.1.test.js" };
                var referenceFiles = new List<ReferencedFile> { referencedFile };

                processor.ClassUnderTest.SetupAmdFilePaths(referenceFiles, testHarnessDirectory, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal("code/test", referencedFile.AmdFilePath);
            }  
        }

        public class GetReferencedFiles
        {

            [Fact]
            public void Will_add_reference_file_to_referenced_files()
            {
                var processor = new TestableReferenceProcessor();
                var settings = new ChutzpahTestSettingsFile { }.InheritFromDefault();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var text = (@"/// <reference path=""lib.js"" />
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\lib.js" && x.IncludeInTestHarness));
            }

            [Fact]
            public void Will_handle_multiple_test_files()
            {
                var processor = new TestableReferenceProcessor();
                var settings = new ChutzpahTestSettingsFile { }.InheritFromDefault();
                var referenceFiles = new List<ReferencedFile> { 
                    new ReferencedFile { IsFileUnderTest = true, Path = @"path\test1.js", ExpandReferenceComments = true },
                    new ReferencedFile { IsFileUnderTest = true, Path = @"path\test2.js", ExpandReferenceComments = true }};
                var text = (@"/// <reference path=""lib.js"" />
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test1.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test2.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.Equal(2, referenceFiles.Count(x => x.IsFileUnderTest));
            }

            [Fact]
            public void Will_exclude_reference_from_harness_in_amd_mode()
            {
                var processor = new TestableReferenceProcessor();
                var settings = new ChutzpahTestSettingsFile { }.InheritFromDefault();
                settings.TestHarnessReferenceMode = TestHarnessReferenceMode.AMD;
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var text = (@"/// <reference path=""lib.js"" />
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\lib.js" && !x.IncludeInTestHarness));
            }

            [Fact]
            public void Will_change_path_root_given_SettingsFileDirectory_RootReferencePathMode()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path1\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile
                {
                    RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory,
                    SettingsFileDirectory = @"C:\root"
                };
                var text = @"/// <reference path=""/this/file.js"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path1\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path.Equals(@"C:\root/this/file.js")));
            }

            [Fact]
            public void Will_change_path_root_given_even_if_has_tilde()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path1\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile
                {
                    RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory,
                    SettingsFileDirectory = @"C:\root"
                };
                var text = @"/// <reference path=""~/this/file.js"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path1\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path.Equals(@"C:\root/this/file.js")));
            }

            [Fact]
            public void Will_not_change_path_root_given_SettingsFileDirectory_RootReferencePathMode()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path1\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {RootReferencePathMode = RootReferencePathMode.DriveRoot, SettingsFileDirectory = @"C:\root"};
                var text = @"/// <reference path=""/this/file.js"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path1\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path.Equals(@"/this/file.js")));
            }

            [Fact]
            public void Will_change_path_root_given_SettingsFileDirectory_RootReferencePathMode_for_html_file()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path1\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile
                {
                    RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory,
                    SettingsFileDirectory = @"C:\root"
                };
                var text = @"/// <reference path=""/this/file.html"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path1\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path.Equals(@"C:\root/this/file.html")));
            }

            [Fact]
            public void Will_not_add_referenced_file_if_it_is_excluded()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path1\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                var text = @"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/excluded.js"" chutzpah-exclude=""true"" />
                        /// <reference path=""../../js/doublenegative.js"" chutzpah-exclude=""false"" />
                        /// <reference path=""../../js/excluded.js"" chutzpahExclude=""true"" />
                        /// <reference path=""../../js/doublenegative.js"" chutzpahExclude=""false"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path1\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.False(referenceFiles.Any(x => x.Path.EndsWith("excluded.js")), "Test context contains excluded reference.");
                Assert.True(referenceFiles.Any(x => x.Path.EndsWith("doublenegative.js")), "Test context does not contain negatively excluded reference.");
            }

            [Fact]
            public void Will_put_recursively_referenced_files_before_parent_file()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(@"/// <reference path=""lib.js"" />");
                string text = @"/// <reference path=""../../js/references.js"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                var ref1 = referenceFiles.First(x => x.Path == @"path\lib.js");
                var ref2 = referenceFiles.First(x => x.Path == @"path\references.js");
                var pos1 = referenceFiles.IndexOf(ref1);
                var pos2 = referenceFiles.IndexOf(ref2);
                Assert.True(pos1 < pos2);
            }

            [Fact]
            public void Will_stop_infinite_loop_when_processing_referenced_files()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                var text = @"/// <reference path=""../../js/references.js"" />
                        some javascript code
                        ";
                var loopText = @"/// <reference path=""../../js/references.js"" />";

                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText(@"path\references.js"))
                    .Returns(loopText);
                processor.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                    .Returns(@"path\references.js");

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\references.js"));
            }

            [Fact]
            public void Will_process_all_files_in_folder_references()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/somefolder")))
                    .Returns((string) null);
                processor.Mock<IFileProbe>()
                    .Setup(x => x.FindFolderPath(Path.Combine(@"path\", @"../../js/somefolder")))
                    .Returns(@"path\someFolder");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"path\someFolder", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] {@"path\subFile.js"});
                var text = @"/// <reference path=""../../js/somefolder"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
            }

            [Fact]
            public void Will_skip_chutzpah_temporary_files_in_folder_references()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileProbe>()
                    .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/somefolder")))
                    .Returns((string) null);
                processor.Mock<IFileProbe>()
                    .Setup(x => x.FindFolderPath(Path.Combine(@"path\", @"../../js/somefolder")))
                    .Returns(@"path\someFolder");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"path\someFolder", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] {@"path\subFile.js"});
                var text = @"/// <reference path=""../../js/somefolder"" />
                        some javascript code
                        ";
                processor.Mock<IFileProbe>().Setup(x => x.IsTemporaryChutzpahFile(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.False(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
            }

            [Fact]
            public void Will_only_include_one_reference_with_mulitple_references_in_html_template()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                var text = (@"/// <template path=""../../templates/file.html"" />
                        /// <template path=""../../templates/file.html"" />
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.Equal(1, referenceFiles.Count(x => x.Path.EndsWith("file.html")));
            }


            [Fact]
            public void Will_parse_html_template_in_script_mode()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile { };
                var text = (@"/// <template mode=""script"" id=""my.Id"" path=""../../templates/file.html"" type=""My/Type""/>
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                var file = referenceFiles.FirstOrDefault(x => x.Path.EndsWith("file.html"));
                Assert.NotNull(file);
                Assert.Equal(TemplateMode.Script, file.TemplateOptions.Mode);
                Assert.Equal("my.Id", file.TemplateOptions.Id);
                Assert.Equal("My/Type", file.TemplateOptions.Type);
            }

            [Fact]
            public void Will_parse_html_template_in_raw_mode()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile { };
                var text = (@"/// <template path=""../../templates/file.html"" />
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                var file = referenceFiles.FirstOrDefault(x => x.Path.EndsWith("file.html"));
                Assert.NotNull(file);
                Assert.Equal(TemplateMode.Raw, file.TemplateOptions.Mode);
                Assert.Null(file.TemplateOptions.Id);
                Assert.Null(file.TemplateOptions.Type);
            }

            [Fact]
            public void Will_add_reference_url_to_referenced_files()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                var text = (@"/// <reference path=""http://a.com/lib.js"" />
                        some javascript code");

                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == "http://a.com/lib.js"));
            }

            [Fact]
            public void Will_add_chutzpah_reference_to_referenced_files()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js", ExpandReferenceComments = true } };
                var settings = new ChutzpahTestSettingsFile {};
                var text = (@"/// <chutzpah_reference path=""lib.js"" />
                        some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path.EndsWith("lib.js")));
            }

            [Fact]
            public void Will_add_file_from_settings_referenced_files()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here.js",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"c:\dir\here.js" && x.IncludeInTestHarness));
            }

            [Fact]
            public void Will_default_path_to_settings_folder_when_adding_from_settings_references()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile().InheritFromDefault();
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\settingsDir")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\settingsDir")).Returns(@"c:\settingsDir");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\settingsDir", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { @"settingsDir\subFile.js", @"settingsDir\newFile.js", @"other\subFile.js" });
                settings.SettingsFileDirectory = @"c:\settingsDir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = null,
                        Include = "*subFile.js",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"settingsDir\subFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
            }

            [Fact]
            public void Will_exclude_from_test_harness_given_setting()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                settings.SettingsFileDirectory = @"c:\dir";
                settings.TestHarnessReferenceMode = TestHarnessReferenceMode.AMD;
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here.js",
                        IncludeInTestHarness = true,
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"c:\dir\here.js" && x.IncludeInTestHarness));
            }

            [Fact]
            public void Will_add_files_from_folder_from_settings_referenced_files()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] {@"path\subFile.js", @"path\newFile.js"});
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
                Assert.True(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
            }

            [Fact]
            public void Will_exclude_files_from_folder_from_settings_referenced_files_if_match_exclude_path()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] {@"path\subFile.js", @"path\newFile.js"});
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here",
                        Exclude = @"*path\sub*",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
            }

            [Fact]
            public void Will_exclude_files_from_folder_from_settings_referenced_files_if_they_dont_match_include_path()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] {@"path\subFile.js", @"path\newFile.js"});
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here",
                        Include = @"*path\sub*",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.False(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
                Assert.True(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
            }

            [Fact]
            public void Will_exclude_files_from_folder_from_settings_referenced_files_if_match_exclude_path_and_dont_match_include()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile {};
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] {@"path\parentFile.js", @"other\newFile.js", @"path\sub\childFile.js"});
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here",
                        Include = @"path\*",
                        Exclude = @"*path\pare*",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\sub\childFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"path\parentFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"other\newFile.js"));
            }

            [Fact]
            public void Will_exclude_files_from_folder_from_settings_referenced_files_if_match_excludes_path_and_dont_match_include()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile { };
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { @"path\parentFile.js", @"other\newFile.js", @"path\sub\childFile.js" });
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here",
                        Includes = new[] { @"path\*", @"other\*", },
                        Excludes = new []{ @"*path\pare*", @"other\new*" },
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\sub\childFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"path\parentFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"other\newFile.js"));
            }

            [Fact]
            public void Will_normlize_paths_for_case_and_slashes_for_path_include_exclude()
            {
                var processor = new TestableReferenceProcessor();
                var referenceFiles = new List<ReferencedFile> { new ReferencedFile { IsFileUnderTest = true, Path = @"path\test.js" } };
                var settings = new ChutzpahTestSettingsFile { };
                processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
                processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
                processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
                processor.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { @"pAth/parentFile.js", @"Other/newFile.js", @"path\sub\childFile.js" });
                settings.SettingsFileDirectory = @"c:\dir";
                settings.References.Add(
                    new SettingsFileReference
                    {
                        Path = "here",
                        Include = @"PATH/*",
                        Exclude = @"*paTh/pAre*",
                        SettingsFileDirectory = settings.SettingsFileDirectory
                    });
                var text = (@"some javascript code");
                processor.Mock<IFileProbe>().Setup(x => x.GetReferencedFileContent(It.Is<ReferencedFile>(f => f.Path == @"path\test.js"), It.IsAny<ChutzpahTestSettingsFile>())).Returns(text);

                processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, settings);

                Assert.True(referenceFiles.Any(x => x.Path == @"path\sub\childFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"path\parentFile.js"));
                Assert.False(referenceFiles.Any(x => x.Path == @"other\newFile.js"));
            }
        }
    }
}