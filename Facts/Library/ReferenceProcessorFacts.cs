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
                frameworkMock.Setup(x => x.TestRunner).Returns("qunitRunner.js");
                frameworkMock.Setup(x => x.TestHarness).Returns("qunit.html");
                frameworkMock.Setup(x => x.FileDependencies).Returns(new[] { "qunit.js", "qunit.css" });
                FrameworkDefinition = frameworkMock.Object;

                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x => x);
                Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns(string.Empty);
            }

        }

        [Fact]
        public void Will_add_reference_file_to_referenced_files()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            var text = (@"/// <reference path=""lib.js"" />
                        some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\lib.js" && x.IncludeInTestHarness));
        }

        [Fact]
        public void Will_exclude_reference_from_harness_in_amd_mode()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            settings.TestHarnessReferenceMode = TestHarnessReferenceMode.AMD; 
            var text = (@"/// <reference path=""lib.js"" />
                        some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\lib.js" && !x.IncludeInTestHarness));
        }        

        [Fact]
        public void Will_change_path_root_given_SettingsFileDirectory_RootReferencePathMode()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory, SettingsFileDirectory = @"C:\root" };
            var text = @"/// <reference path=""/this/file.js"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path1\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path.Equals(@"C:\root/this/file.js")));
        }

        [Fact]
        public void Will_change_path_root_given_even_if_has_tilde()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory, SettingsFileDirectory = @"C:\root" };
            var text = @"/// <reference path=""~/this/file.js"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path1\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path.Equals(@"C:\root/this/file.js")));
        }

        [Fact]
        public void Will_not_change_path_root_given_SettingsFileDirectory_RootReferencePathMode()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { RootReferencePathMode = RootReferencePathMode.DriveRoot, SettingsFileDirectory = @"C:\root" };
            var text = @"/// <reference path=""/this/file.js"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path1\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path.Equals(@"/this/file.js")));
        }

        [Fact]
        public void Will_change_path_root_given_SettingsFileDirectory_RootReferencePathMode_for_html_file()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory, SettingsFileDirectory = @"C:\root" };
            var text = @"/// <reference path=""/this/file.html"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path1\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path.Equals(@"C:\root/this/file.html")));
        }

        [Fact]
        public void Will_not_add_referenced_file_if_it_is_excluded()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            var text = @"/// <reference path=""lib.js"" />
                        /// <reference path=""../../js/excluded.js"" chutzpah-exclude=""true"" />
                        /// <reference path=""../../js/doublenegative.js"" chutzpah-exclude=""false"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path1\test.js", settings);

            Assert.False(referenceFiles.Any(x => x.Path.EndsWith("excluded.js")), "Test context contains excluded reference.");
            Assert.True(referenceFiles.Any(x => x.Path.EndsWith("doublenegative.js")), "Test context does not contain negatively excluded reference.");
        }

        [Fact]
        public void Will_put_recursively_referenced_files_before_parent_file()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileProbe>()
                .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                .Returns(@"path\references.js");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetText(@"path\references.js"))
                .Returns(@"/// <reference path=""lib.js"" />");
            string text = @"/// <reference path=""../../js/references.js"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            var ref1 = referenceFiles.First(x => x.Path == @"path\lib.js");
            var ref2 = referenceFiles.First(x => x.Path == @"path\references.js");
            var pos1 = referenceFiles.IndexOf(ref1);
            var pos2 = referenceFiles.IndexOf(ref2);
            Assert.True(pos1 < pos2);
        }

        [Fact(Timeout = 5000)]
        public void Will_stop_infinite_loop_when_processing_referenced_files()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            var text = @"/// <reference path=""../../js/references.js"" />
                        some javascript code
                        ";
            var loopText = @"/// <reference path=""../../js/references.js"" />";

            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetText(@"path\references.js"))
                .Returns(loopText);
            processor.Mock<IFileProbe>()
                .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/references.js")))
                .Returns(@"path\references.js");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\references.js"));
        }
        
        [Fact]
        public void Will_process_all_files_in_folder_references()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileProbe>()
                .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/somefolder")))
                .Returns((string)null);
            processor.Mock<IFileProbe>()
                .Setup(x => x.FindFolderPath(Path.Combine(@"path\", @"../../js/somefolder")))
                .Returns(@"path\someFolder");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetFiles(@"path\someFolder", "*.*", SearchOption.AllDirectories))
                .Returns(new[] { @"path\subFile.js" });
            var text = @"/// <reference path=""../../js/somefolder"" />
                        some javascript code
                        ";

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
        }

        [Fact]
        public void Will_skip_chutzpah_temporary_files_in_folder_references()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileProbe>()
                .Setup(x => x.FindFilePath(Path.Combine(@"path\", @"../../js/somefolder")))
                .Returns((string)null);
            processor.Mock<IFileProbe>()
                .Setup(x => x.FindFolderPath(Path.Combine(@"path\", @"../../js/somefolder")))
                .Returns(@"path\someFolder");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetFiles(@"path\someFolder", "*.*", SearchOption.AllDirectories))
                .Returns(new[] { @"path\subFile.js" });
            var text = @"/// <reference path=""../../js/somefolder"" />
                        some javascript code
                        ";
            processor.Mock<IFileProbe>().Setup(x => x.IsTemporaryChutzpahFile(It.IsAny<string>())).Returns(true);

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.False(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
        }

        [Fact]
        public void Will_only_include_one_reference_with_mulitple_references_in_html_template()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            var text = (@"/// <template path=""../../templates/file.html"" />
                        /// <template path=""../../templates/file.html"" />
                        some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.Equal(1, referenceFiles.Count(x => x.Path.EndsWith("file.html")));
        }

        [Fact]
        public void Will_add_reference_url_to_referenced_files()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            var text = (@"/// <reference path=""http://a.com/lib.js"" />
                        some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == "http://a.com/lib.js"));
        }        
        
        [Fact]
        public void Will_add_chutzpah_reference_to_referenced_files()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            var text = (@"/// <chutzpah_reference path=""lib.js"" />
                        some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path.EndsWith("lib.js")));
        }

        [Fact]
        public void Will_add_file_from_settings_referenced_files()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            settings.SettingsFileDirectory = @"c:\dir";
            settings.References.Add(new SettingsFileReference
            {
                Path = "here.js"
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"c:\dir\here.js" && x.IncludeInTestHarness));
        }

        [Fact]
        public void Will_exclude_from_test_harness_given_setting()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            settings.SettingsFileDirectory = @"c:\dir";
            settings.TestHarnessReferenceMode = TestHarnessReferenceMode.AMD;
            settings.References.Add(new SettingsFileReference
            {
                Path = "here.js",
                AlwaysIncludeInTestHarness = true
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"c:\dir\here.js" && x.IncludeInTestHarness));
        }

        [Fact]
        public void Will_add_files_from_folder_from_settings_referenced_files()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
            processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                .Returns(new[] { @"path\subFile.js", @"path\newFile.js" });
            settings.SettingsFileDirectory = @"c:\dir";
            settings.References.Add(new SettingsFileReference
            {
                Path = "here"
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
            Assert.True(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
        }

        [Fact]
        public void Will_exclude_files_from_folder_from_settings_referenced_files_if_match_exclude_path()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
            processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                .Returns(new[] { @"path\subFile.js", @"path\newFile.js" });
            settings.SettingsFileDirectory = @"c:\dir";
            settings.References.Add(new SettingsFileReference
            {
                Path = "here",
                Exclude = @"*path\sub*"
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
            Assert.False(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
        }

        [Fact]
        public void Will_exclude_files_from_folder_from_settings_referenced_files_if_they_dont_match_include_path()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
            processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                .Returns(new[] { @"path\subFile.js", @"path\newFile.js" });
            settings.SettingsFileDirectory = @"c:\dir";
            settings.References.Add(new SettingsFileReference
            {
                Path = "here",
                Include = @"*path\sub*"
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.False(referenceFiles.Any(x => x.Path == @"path\newFile.js"));
            Assert.True(referenceFiles.Any(x => x.Path == @"path\subFile.js"));
        }

        [Fact]
        public void Will_exclude_files_from_folder_from_settings_referenced_files_if_match_exclude_path_and_dont_match_include()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\here")).Returns<string>(null);
            processor.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"c:\dir\here")).Returns(@"c:\dir\here");
            processor.Mock<IFileSystemWrapper>()
                .Setup(x => x.GetFiles(@"c:\dir\here", "*.*", SearchOption.AllDirectories))
                .Returns(new[] { @"path\parentFile.js", @"other\newFile.js", @"path\sub\childFile.js" });
            settings.SettingsFileDirectory = @"c:\dir";
            settings.References.Add(new SettingsFileReference
            {
                Path = "here",
                Include = @"path\*",
                Exclude = @"*path\pare*"
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"path\sub\childFile.js"));
            Assert.False(referenceFiles.Any(x => x.Path == @"path\parentFile.js"));
            Assert.False(referenceFiles.Any(x => x.Path == @"other\newFile.js"));
        }

        [Fact]
        public void Will_add_reference_file_from_included_libraries()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\chai.js")).Returns<string>(null);
            var text = (@"/// <reference path=""chai.js"" />
                        some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"TestFiles\IncludedLibraries\chai.js"));
        }

        [Fact]
        public void Will_add_file_from_included_files_from_settings()
        {
            var processor = new TestableReferenceProcessor();
            var referenceFiles = new List<ReferencedFile>();
            var settings = new ChutzpahTestSettingsFile { };
            processor.Mock<IFileSystemWrapper>().Setup(x => x.FolderExists(It.IsAny<string>())).Returns(true);
            processor.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"c:\dir\chai.js")).Returns<string>(null);
            settings.SettingsFileDirectory = @"c:\dir";
            settings.References.Add(new SettingsFileReference
            {
                Path = "chai.js"
            });
            var text = (@"some javascript code");

            processor.ClassUnderTest.GetReferencedFiles(referenceFiles, processor.FrameworkDefinition, text, @"path\test.js", settings);

            Assert.True(referenceFiles.Any(x => x.Path == @"TestFiles\IncludedLibraries\chai.js"));
        }
    }
}