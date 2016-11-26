using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Library.Models
{
    public class ChutzpahTestSettingsServiceFacts
    {
        private class TestableChutzpahTestSettingsService : Testable<ChutzpahTestSettingsService>
        {
            public TestableChutzpahTestSettingsService()
            {

                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns<string>(x=> x);
                Mock<IFileProbe>().Setup(x => x.FindFolderPath(It.IsAny<string>())).Returns<string>(x=> x);

                this.ClassUnderTest.ClearCache();
            }
        }

        [Fact]
        public void Will_set_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir");

            Assert.Equal(@"C:\settingsDir", settings.SettingsFileDirectory);
        }

        [Fact]
        public void Will_set_custom_harness_directory_based_relative_to_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile { TestHarnessLocationMode = TestHarnessLocationMode.Custom, TestHarnessDirectory = "custom" };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir2\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir2\custom")).Returns(@"customPath");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir2");

            Assert.Equal(@"customPath", settings.TestHarnessDirectory);
        }

        [Fact]
        public void Will_set_amdbasepath_based_relative_to_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile { AMDBasePath = "custom" };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir6\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir6\custom")).Returns(@"customPath");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir6");

            Assert.Equal(@"customPath", settings.AMDBasePath);
        }

        [Fact]
        public void Will_get_cached_settings_given_same_starting_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile("dir3")).Returns(@"C:\settingsDir3\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);
            service.ClassUnderTest.FindSettingsFileFromDirectory("dir3");

            var cached = service.ClassUnderTest.FindSettingsFileFromDirectory("dir3");

            Assert.Equal(@"C:\settingsDir3", cached.SettingsFileDirectory);
        }

        [Fact]
        public void Will_get_cached_settings_given_same_settings_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile("dir4")).Returns(@"C:\settingsDir4\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);
            service.ClassUnderTest.FindSettingsFileFromDirectory("dir4");

            var cached = service.ClassUnderTest.FindSettingsFileFromDirectory(@"C:\settingsDir4\");

            Assert.Equal(@"C:\settingsDir4", cached.SettingsFileDirectory);
        }

        [Fact]
        public void Will_cache_missing_default_settings_for_missing_settings_files()
        {
            var service = new TestableChutzpahTestSettingsService();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile("dir5")).Returns((string)null);
            service.ClassUnderTest.FindSettingsFileFromDirectory(@"dir5");

            var cached = service.ClassUnderTest.FindSettingsFileFromDirectory(@"dir5");

            service.Mock<IFileProbe>().Verify(x => x.FindTestSettingsFile("dir5"), Times.Once());
        }

        [Fact]
        public void Will_set_compile_configuration_paths_based_relative_to_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = "executable",
                    WorkingDirectory = "work",
                    Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = "source", OutputPath = "out" } }
                }
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\settingsDir7\executable")).Returns(@"customPath1");
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir7\work")).Returns(@"customPath2");
            service.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\settingsDir7\source")).Returns<string>(null);
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir7\source")).Returns(@"customPath3");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7");

            Assert.Equal(@"customPath1", settings.Compile.Executable);
            Assert.Equal(@"customPath2", settings.Compile.WorkingDirectory);
            Assert.Equal(@"customPath3", settings.Compile.Paths.First().SourcePath);
            Assert.False(settings.Compile.Paths.First().SourcePathIsFile);
            Assert.Equal(@"c:\settingsdir7\out", settings.Compile.Paths.First().OutputPath);
            Assert.False(settings.Compile.Paths.First().OutputPathIsFile);
        }

        [Fact]
        public void Will_set_isFile_for_source_path()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = "executable",
                    WorkingDirectory = "work",
                    Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = "source", OutputPath = "out" } }
                }
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\settingsDir7\source")).Returns(@"customPath3");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7");
            
            Assert.Equal(@"customPath3", settings.Compile.Paths.First().SourcePath);
            Assert.True(settings.Compile.Paths.First().SourcePathIsFile);
            Assert.False(settings.Compile.Paths.First().OutputPathIsFile);
        }

        [Fact]
        public void Will_set_isFile_for_output_if_path_type_is_file()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = "executable",
                    WorkingDirectory = "work",
                    Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = "source", OutputPath = "out", OutputPathType = CompilePathType.File } }
                }
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7");
            
            Assert.Equal(@"c:\settingsdir7\out", settings.Compile.Paths.First().OutputPath);
            Assert.True(settings.Compile.Paths.First().OutputPathIsFile);
        }


        [Fact]
        public void Will_set_isFile_for_output_if_path_ends_in_js()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = "executable",
                    WorkingDirectory = "work",
                    Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = "source", OutputPath = "out.js", OutputPathType = null } }
                }
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7");

            Assert.Equal(@"c:\settingsdir7\out.js", settings.Compile.Paths.First().OutputPath);
            Assert.True(settings.Compile.Paths.First().OutputPathIsFile);
        }

        [Theory]
        [InlineData("clrdir", false)]
        [InlineData("msbuildexe", false)]
        [InlineData("powershellexe", false)]
        [InlineData("cmdexe", false)]
        [InlineData("comspec", false)]
        [InlineData("chutzpahsettingsdir", false)]
        [InlineData("blah", true)]
        public void Will_expand_chutzpah_and_default_environment_variables(string variable, bool result)
        {
            var service = new TestableChutzpahTestSettingsService();
            var varStr = string.Format("%{0}%", variable);
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = string.Format("path {0} ok", varStr),
                    Arguments = string.Format("path {0} ok", varStr),

                    Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = "source", OutputPath = string.Format("path {0} ok", varStr) } }
                }
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7");

            Assert.Equal(result, settings.Compile.Executable.Contains(varStr));
            Assert.Equal(result, settings.Compile.Arguments.Contains(varStr));
            Assert.Equal(result, settings.Compile.Paths.First().OutputPath.Contains(varStr));
        }

        [Fact]
        public void Will_expand_variables_from_passed_in_environment()
        {
            var service = new TestableChutzpahTestSettingsService();
            var environment = new ChutzpahSettingsFileEnvironment("path");
            environment.Path = @"dir7";
            environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty("SomeName", "SomeValue"));
            var varStr = "%SomeName%";
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = string.Format("path {0} ok", varStr),
                    Arguments = string.Format("path {0} ok", varStr),
                    Paths = new List<CompilePathMap> { new CompilePathMap { OutputPath = string.Format("path {0} ok", varStr) } },
                },
                References = new []{new SettingsFileReference{ Path = varStr, Include = varStr, Exclude = varStr}},
                Tests = new []{new SettingsFileTestPath{ Path = varStr, Include = varStr, Exclude = varStr}},
                Transforms = new []{ new TransformConfig{ Path = varStr}},
                AMDBasePath = varStr,
                AMDBaseUrl = varStr
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7", new ChutzpahSettingsFileEnvironments(new []{environment}));

            Assert.True(settings.Compile.Executable.Contains("SomeValue"));
            Assert.True(settings.Compile.Arguments.Contains("SomeValue"));
            Assert.True(settings.Compile.Paths.First().OutputPath.Contains("somevalue"));
            Assert.True(settings.References.ElementAt(0).Path.Contains("SomeValue"));
            Assert.True(settings.References.ElementAt(0).Includes[0].Contains("SomeValue"));
            Assert.True(settings.References.ElementAt(0).Excludes[0].Contains("SomeValue"));
            Assert.True(settings.Tests.ElementAt(0).Path.Contains("SomeValue"));
            Assert.True(settings.Tests.ElementAt(0).Includes[0].Contains("SomeValue"));
            Assert.True(settings.Tests.ElementAt(0).Excludes[0].Contains("SomeValue"));
            Assert.True(settings.Transforms.ElementAt(0).Path.Contains("SomeValue"));
            Assert.True(settings.AMDBasePath.Contains("SomeValue"));
            Assert.True(settings.AMDBaseUrl.Contains("SomeValue"));
        }

        [Fact]
        public void Will_inherit_all_parent_settings_when_none_set_on_child()
        {
            var service = new TestableChutzpahTestSettingsService();

            var parentSettings = new ChutzpahTestSettingsFile
            {
                Tests = new List<SettingsFileTestPath> { new SettingsFileTestPath { Path = "parentTestPath" } },
                References = new List<SettingsFileReference> { new SettingsFileReference { Path = "parentReferencePath" } },
                Transforms = new List<TransformConfig>{ new TransformConfig{ Path = "parentTransformPath"}},
                CodeCoverageExcludes = new List<string>{"parentCodeCoverageExcludePath"},
                CodeCoverageIncludes = new List<string> { "parentCodeCoverageIncludePath" },
                CodeCoverageIgnores = new List<string> { "parentCodeCoverageIgnorePath" },
                Compile = new BatchCompileConfiguration{ Mode = BatchCompileMode.External},
                
                AMDBasePath = "parentAmdBasePath",
                CodeCoverageExecutionMode = CodeCoverageExecutionMode.Always,
                Framework = "parentFramework",
                FrameworkVersion = "parentVersion",
                TestHarnessDirectory = "parentHarnessDirectory",
                TestHarnessLocationMode = TestHarnessLocationMode.Custom,
                TestHarnessReferenceMode = TestHarnessReferenceMode.AMD,
                RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory,
                TestPattern = "parentTestPattern",
                TestFileTimeout = 10,
                UserAgent = "parentUserAgent"
            };

            var childSettings = new ChutzpahTestSettingsFile
            {
                InheritFromParent = true
            };

            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:\settingsDir")).Returns(@"C:\settingsDir\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:")).Returns(@"C:\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsDir\settingsFile.json")).Returns(childSettings);
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsFile.json")).Returns(parentSettings);

            service.ClassUnderTest.FindSettingsFileFromDirectory(@"C:\settingsDir");


            // Tests to run are not inherited
            Assert.Equal(0, childSettings.Tests.Count);

            Assert.Equal(1, childSettings.References.Count);
            Assert.Equal(1, childSettings.Transforms.Count);
            Assert.Equal(1, childSettings.CodeCoverageIncludes.Count);
            Assert.Equal(1, childSettings.CodeCoverageExcludes.Count);
            Assert.Equal(1, childSettings.CodeCoverageIgnores.Count);

            Assert.Equal(parentSettings.Compile, childSettings.Compile);
            Assert.Equal(@"C:\settingsDir", childSettings.SettingsFileDirectory);
            Assert.Equal(@"C:\parentAmdBasePath", childSettings.AMDBasePath);
            Assert.Equal(CodeCoverageExecutionMode.Always, childSettings.CodeCoverageExecutionMode);
            Assert.Equal(@"parentFramework", childSettings.Framework);
            Assert.Equal(@"parentVersion", childSettings.FrameworkVersion);
            Assert.Equal(@"C:\parentHarnessDirectory", childSettings.TestHarnessDirectory);
            Assert.Equal(TestHarnessLocationMode.Custom, childSettings.TestHarnessLocationMode);
            Assert.Equal(TestHarnessReferenceMode.AMD, childSettings.TestHarnessReferenceMode);
            Assert.Equal(RootReferencePathMode.SettingsFileDirectory, childSettings.RootReferencePathMode);
            Assert.Equal("parentTestPattern", childSettings.TestPattern);
            Assert.Equal(10, childSettings.TestFileTimeout);
            Assert.Equal("parentUserAgent", childSettings.UserAgent);
        }


        [Fact]
        public void Will_merge_list_settings_and_set_settings_dir_property_for_applicable_settings()
        {
            var service = new TestableChutzpahTestSettingsService();

            var parentSettings = new ChutzpahTestSettingsFile
            {
                Tests = new List<SettingsFileTestPath> { new SettingsFileTestPath { Path = "parentTestPath" } },
                References = new List<SettingsFileReference> { new SettingsFileReference { Path = "parentReferencePath" } },
                Transforms = new List<TransformConfig> { new TransformConfig { Path = "parentTransformPath" } },
                CodeCoverageExcludes = new List<string> { "parentCodeCoverageExcludePath" },
                CodeCoverageIncludes = new List<string> { "parentCodeCoverageIncludePath" },
                CodeCoverageIgnores = new List<string> { "parentCodeCoverageIgnorePath" }
            };

            var childSettings = new ChutzpahTestSettingsFile
            {
                InheritFromParent = true,
                Tests = new List<SettingsFileTestPath> { new SettingsFileTestPath { Path = "childTestPath" } },
                References = new List<SettingsFileReference> { new SettingsFileReference { Path = "childReferencePath" } },
                Transforms = new List<TransformConfig> { new TransformConfig { Path = "childTransformPath" } },
                CodeCoverageExcludes = new List<string> { "childCodeCoverageExcludePath" },
                CodeCoverageIncludes = new List<string> { "childCodeCoverageIncludePath" },
                CodeCoverageIgnores = new List<string> { "childCodeCoverageIgnorePath" }
            };

            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:\settingsDir")).Returns(@"C:\settingsDir\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:")).Returns(@"C:\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsDir\settingsFile.json")).Returns(childSettings);
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsFile.json")).Returns(parentSettings);

            service.ClassUnderTest.FindSettingsFileFromDirectory(@"C:\settingsDir");

            // Tests to run are not inherited
            Assert.Equal(1, childSettings.Tests.Count);
            Assert.Equal("childTestPath", childSettings.Tests.ElementAt(0).Path);
            Assert.Equal(@"C:\settingsDir", childSettings.Tests.ElementAt(0).SettingsFileDirectory);

            Assert.Equal(2, childSettings.References.Count);
            Assert.Equal("parentReferencePath", childSettings.References.ElementAt(0).Path);
            Assert.Equal(@"C:\", childSettings.References.ElementAt(0).SettingsFileDirectory);
            Assert.Equal("childReferencePath", childSettings.References.ElementAt(1).Path);
            Assert.Equal(@"C:\settingsDir", childSettings.References.ElementAt(1).SettingsFileDirectory);

            Assert.Equal(2, childSettings.Transforms.Count);
            Assert.Equal("parentTransformPath", childSettings.Transforms.ElementAt(0).Path);
            Assert.Equal(@"C:\", childSettings.Transforms.ElementAt(0).SettingsFileDirectory);
            Assert.Equal("childTransformPath", childSettings.Transforms.ElementAt(1).Path);
            Assert.Equal(@"C:\settingsDir", childSettings.Transforms.ElementAt(1).SettingsFileDirectory);

            Assert.Equal(2, childSettings.CodeCoverageIncludes.Count);
            Assert.Equal("parentCodeCoverageIncludePath", childSettings.CodeCoverageIncludes.ElementAt(0));
            Assert.Equal("childCodeCoverageIncludePath", childSettings.CodeCoverageIncludes.ElementAt(1));
            Assert.Equal(2, childSettings.CodeCoverageExcludes.Count);
            Assert.Equal("parentCodeCoverageExcludePath", childSettings.CodeCoverageExcludes.ElementAt(0));
            Assert.Equal("childCodeCoverageExcludePath", childSettings.CodeCoverageExcludes.ElementAt(1));
            Assert.Equal(2, childSettings.CodeCoverageIgnores.Count);
            Assert.Equal("parentCodeCoverageIgnorePath", childSettings.CodeCoverageIgnores.ElementAt(0));
            Assert.Equal("childCodeCoverageIgnorePath", childSettings.CodeCoverageIgnores.ElementAt(1));
        }

        [Fact]
        public void Will_merge_settings_between_parent_and_child()
        {
            var service = new TestableChutzpahTestSettingsService();

            var parentSettings = new ChutzpahTestSettingsFile
            {
                // Parent Only
                AMDBasePath = "parentAmdBasePath",
                CodeCoverageExecutionMode = CodeCoverageExecutionMode.Always,
                Framework = "parentFramework",

                // Both parent and child
                FrameworkVersion = "parentVersion",
                TestHarnessDirectory = "parentHarnessDirectory",
                TestHarnessLocationMode = TestHarnessLocationMode.Custom,
                TestHarnessReferenceMode = TestHarnessReferenceMode.AMD,
                RootReferencePathMode = RootReferencePathMode.SettingsFileDirectory,
                Compile = new BatchCompileConfiguration { Mode = BatchCompileMode.External },
                Server = new ChutzpahWebServerConfiguration()
            };

            var childSettings = new ChutzpahTestSettingsFile
            {
                InheritFromParent = true,

                // Both parent and child
                FrameworkVersion = "childVersion",
                TestHarnessLocationMode = TestHarnessLocationMode.TestFileAdjacent,
                TestHarnessReferenceMode = TestHarnessReferenceMode.Normal,
                RootReferencePathMode = RootReferencePathMode.DriveRoot,
                Compile = new BatchCompileConfiguration { Mode = BatchCompileMode.External },

                TestPattern = "childTestPattern",
                TestFileTimeout = 11,
                UserAgent = "childUserAgent",
                Server = new ChutzpahWebServerConfiguration()
            };

            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:\settingsDir")).Returns(@"C:\settingsDir\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:")).Returns(@"C:\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsDir\settingsFile.json")).Returns(childSettings);
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsFile.json")).Returns(parentSettings);

            service.ClassUnderTest.FindSettingsFileFromDirectory(@"C:\settingsDir");

            Assert.Equal(@"C:\settingsDir", childSettings.SettingsFileDirectory);

            Assert.Equal(@"C:\parentAmdBasePath", childSettings.AMDBasePath);
            Assert.Equal(CodeCoverageExecutionMode.Always, childSettings.CodeCoverageExecutionMode);
            Assert.Equal(@"parentFramework", childSettings.Framework);

            Assert.NotEqual(parentSettings.Compile, childSettings.Compile);
            Assert.Equal(@"childVersion", childSettings.FrameworkVersion);
            Assert.Equal(null, childSettings.TestHarnessDirectory);
            Assert.Equal(TestHarnessLocationMode.TestFileAdjacent, childSettings.TestHarnessLocationMode);
            Assert.Equal(TestHarnessReferenceMode.Normal, childSettings.TestHarnessReferenceMode);
            Assert.Equal(RootReferencePathMode.DriveRoot, childSettings.RootReferencePathMode);

            Assert.Equal("childTestPattern", childSettings.TestPattern);
            Assert.Equal(11, childSettings.TestFileTimeout);
            Assert.Equal("childUserAgent", childSettings.UserAgent);

            if (!ChutzpahTestSettingsFile.ForceWebServerMode)
            {
                Assert.Equal(parentSettings.Server.RootPath, childSettings.Server.RootPath);
                Assert.Equal(parentSettings.Server.Enabled, childSettings.Server.Enabled);
                Assert.Equal(parentSettings.Server.DefaultPort, childSettings.Server.DefaultPort);
                Assert.Equal(parentSettings.Server.FileCachingEnabled, childSettings.Server.FileCachingEnabled);
            }
        }


        [Fact]
        public void Will_convert_parent_testharnesslocationmode_to_custom_in_child_if_settingsfileadjacent()
        {
            var service = new TestableChutzpahTestSettingsService();

            var parentSettings = new ChutzpahTestSettingsFile
            {
                TestHarnessLocationMode = TestHarnessLocationMode.SettingsFileAdjacent
            };

            var childSettings = new ChutzpahTestSettingsFile
            {
                InheritFromParent = true
            };

            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:\settingsDir")).Returns(@"C:\settingsDir\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(@"C:")).Returns(@"C:\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsDir\settingsFile.json")).Returns(childSettings);
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(@"C:\settingsFile.json")).Returns(parentSettings);

            service.ClassUnderTest.FindSettingsFileFromDirectory(@"C:\settingsDir");

            Assert.Equal(@"C:\settingsDir", childSettings.SettingsFileDirectory);
            Assert.Equal(@"C:\", childSettings.TestHarnessDirectory);
            Assert.Equal(TestHarnessLocationMode.Custom, childSettings.TestHarnessLocationMode);
    
        }

        [Fact]
        public void Will_map_deprecated_sourcedirectory_and_outdirectory_to_paths_setting()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile
            {
                Compile = new BatchCompileConfiguration
                {
                    Executable = "executable",
                    WorkingDirectory = "work",
                    SourceDirectory = "source",
                    OutDirectory = "out",
                }
            };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir7\settingsFile.json");

            service.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"C:\settingsDir7\source")).Returns((string)null);
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir7\source")).Returns(@"customPath3");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFileFromDirectory("dir7");
            
            Assert.Equal(@"customPath3", settings.Compile.Paths.First().SourcePath);
            Assert.Equal(@"c:\settingsdir7\out", settings.Compile.Paths.First().OutputPath);
        }
    }
}
