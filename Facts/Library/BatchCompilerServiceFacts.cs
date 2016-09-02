using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.BatchProcessor;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;
using Chutzpah.FileProcessors;
using System.IO;

namespace Chutzpah.Facts
{
    public class BatchCompilerServiceFacts
    {
        public class TestableBatchCompilerService : Testable<BatchCompilerService>
        {
            public TestableBatchCompilerService()
            {

                Mock<IProcessHelper>().Setup(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>())).Returns(new BatchCompileResult());
                Mock<ISourceMapDiscoverer>().Setup(x => x.FindSourceMap(@"C:\src\a.js")).Returns(@"C:\src\a.js.map");
            }

            public TestContext BuildContext()
            {
                var context = new TestContext();
                context.TestFileSettings = new ChutzpahTestSettingsFile
                {
                    SettingsFileDirectory = @"C:\src",
                    Compile = new BatchCompileConfiguration
                    {
                        Mode = BatchCompileMode.Executable,
                        Executable = "compile.bat",
                        SkipIfUnchanged = true,
                        WorkingDirectory = @"C:\src",
                        Paths = new List<CompilePathMap>
                        {
                            new CompilePathMap { SourcePath = @"C:\src", OutputPath = @"C:\src" }
                        }
                    }
                }.InheritFromDefault();

                return context;
            }
        }

        [Fact]
        public void Will_not_compile_if_no_compile_setting()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile = null;
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js")))).Returns(false).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()), Times.Never());
            Assert.Null(context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
        }

        [Fact]
        public void Will_compile_and_mark_generate_paths_from_one_context_when_output_missing()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\c.js" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))))
                .Returns(false)
                .Returns(false)
                .Returns(true)
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()));
            Assert.Equal(@"C:\src\a.js", context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
            Assert.Equal(@"C:\src\b.js", context.ReferencedFiles.ElementAt(1).GeneratedFilePath);
            Assert.Null(context.ReferencedFiles.ElementAt(2).GeneratedFilePath);
        }

        [Fact]
        public void Will_throw_if_file_is_missing()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.Mode = BatchCompileMode.External;
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\c.js" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))))
                .Returns(false)
                .Returns(true)
                .Returns(false)
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            var callback = service.Mock<ITestMethodRunnerCallback>();

            service.ClassUnderTest.Compile(new[] { context }, callback.Object);

            callback.Verify(x => x.ExceptionThrown(It.IsAny<FileNotFoundException>(), It.IsAny<string>()));
            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()), Times.Never());
        }

        [Fact]
        public void Will_mark_generated_path_when_output_folder_is_set()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = @"C:\src", OutputPath = @"C:\out" } };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))))
                .Returns(false)
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            Assert.Equal(@"C:\out\a.js", context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
        }

        [Fact]
        public void Will_mark_generated_path_when_source_folder_is_set()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = @"C:\other", OutputPath = @"C:\src" } };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\other\a.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".js"))))
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            Assert.Equal(@"C:\src\a.js", context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
        }

        [Fact]
        public void Will_attempt_colocation_if_initially_not_found()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = @"C:\other", OutputPath = @"C:\src" } };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\hello\a.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".js"))))
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            Assert.Equal(@"C:\hello\a.js", context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
        }


        [Fact]
        public void Will_set_source_map_path_when_UseSourceMap_setting_enabled()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = @"C:\other", OutputPath = @"C:\src" } };
            context.TestFileSettings.Compile.UseSourceMaps = true;
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\other\a.ts" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".js"))))
                .Returns(false)
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            Assert.Equal(@"C:\src\a.js.map", context.ReferencedFiles.ElementAt(0).SourceMapFilePath);
        }

        [Fact]
        public void Will_not_set_source_map_path_when_UseSourceMap_setting_disabled()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = @"C:\other", OutputPath = @"C:\src" } };
            context.TestFileSettings.Compile.UseSourceMaps = false;
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\other\a.ts" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".js"))))
                .Returns(false)
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f != null && f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            Assert.Null(context.ReferencedFiles.ElementAt(0).SourceMapFilePath);
            service.Mock<ISourceMapDiscoverer>().Verify(x => x.FindSourceMap(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void Will_not_look_for_generate_output_for_extensions_which_wont_have()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.ExtensionsWithNoOutput = new[] { ".d.ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.d.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))), Times.Never());
            Assert.Null(context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
        }

        [Fact]
        public void Will_skip_compile_if_all_files_have_up_to_date_output()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.ts")).Returns(DateTime.Now.AddDays(-1));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.js")).Returns(DateTime.Now);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.ts")).Returns(DateTime.Now);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.js")).Returns(DateTime.Now);

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()), Times.Never());
        }

        [Fact]
        public void Will_compile_if_source_file_is_newer_than_output()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.ts")).Returns(DateTime.Now.AddDays(1));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.js")).Returns(DateTime.Now);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.ts")).Returns(DateTime.Now);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.js")).Returns(DateTime.Now);

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()));
        }

        [Fact]
        public void Will_compile_if_souce_who_makes_no_output_is_newer_than_oldest_output()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.ExtensionsWithNoOutput = new[] { ".d.ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\c.d.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\c.d.ts")).Returns(DateTime.Now.AddDays(-2));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.ts")).Returns(DateTime.Now.AddDays(-4));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.js")).Returns(DateTime.Now.AddDays(-3));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.ts")).Returns(DateTime.Now.AddDays(-5));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.js")).Returns(DateTime.Now.AddDays(-1));

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()));
        }

        [Fact]
        public void Will_not_compile_if_souce_who_makes_no_output_is_not_newer_than_oldest_output()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.TestFileSettings.Compile.ExtensionsWithNoOutput = new[] { ".d.ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\c.d.ts" });
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\c.d.ts")).Returns(DateTime.Now.AddDays(-3));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.ts")).Returns(DateTime.Now.AddDays(-4));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\a.js")).Returns(DateTime.Now.AddDays(-2));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.ts")).Returns(DateTime.Now.AddDays(-5));
            service.Mock<IFileSystemWrapper>().Setup(x => x.GetLastWriteTime(@"C:\src\b.js")).Returns(DateTime.Now.AddDays(-1));

            service.ClassUnderTest.Compile(new[] { context });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()), Times.Never());
        }

        [Fact]
        public void Will_throw_if_process_returns_non_zero_exit_code()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))))
                .Returns(false)
                .Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);
            service.Mock<IProcessHelper>()
                .Setup(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()))
                .Returns(new BatchCompileResult
                {
                    ExitCode = 1
                });

            var ex = Record.Exception(() => service.ClassUnderTest.Compile(new[] { context }));

            Assert.IsType<ChutzpahCompilationFailedException>(ex);
        }

        [Fact]
        public void Will_compile_multiple_contexts_which_have_one_settings_file()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\c.js" });
            var context2 = service.BuildContext();
            context2.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context2.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context2.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\d.ts" });
            context2.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\c.js" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))))
                .Returns(false).Returns(false).Returns(false)
                .Returns(true).Returns(true).Returns(true).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context, context2 });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()), Times.Once());
            Assert.Equal(@"C:\src\a.js", context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
            Assert.Equal(@"C:\src\b.js", context.ReferencedFiles.ElementAt(1).GeneratedFilePath);
            Assert.Null(context.ReferencedFiles.ElementAt(2).GeneratedFilePath);
            Assert.Equal(@"C:\src\a.js", context2.ReferencedFiles.ElementAt(0).GeneratedFilePath);
            Assert.Equal(@"C:\src\d.js", context2.ReferencedFiles.ElementAt(1).GeneratedFilePath);
            Assert.Null(context2.ReferencedFiles.ElementAt(2).GeneratedFilePath);
        }

        [Fact]
        public void Will_compile_multiple_contexts_which_have_different_settings_file()
        {
            var service = new TestableBatchCompilerService();
            var context = service.BuildContext();
            context.TestFileSettings.SettingsFileDirectory = @"C:\src";
            context.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\a.ts" });
            context.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\src\b.ts" });
            var context2 = service.BuildContext();
            context2.TestFileSettings.SettingsFileDirectory = @"C:\other";
            context2.TestFileSettings.Compile.Paths = new List<CompilePathMap> { new CompilePathMap { SourcePath = @"C:\other", OutputPath = @"C:\other" } };
            context2.TestFileSettings.Compile.Extensions = new[] { ".ts" };
            context2.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\other\a.ts" });
            context2.ReferencedFiles.Add(new ReferencedFile { Path = @"C:\other\d.ts" });
            service.Mock<IFileSystemWrapper>().SetupSequence(x => x.FileExists(It.Is<string>(f => f.EndsWith(".js"))))
                .Returns(false).Returns(false).Returns(true).Returns(true)
                .Returns(false).Returns(false).Returns(true).Returns(true);
            service.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(It.Is<string>(f => f.EndsWith(".ts")))).Returns(true);

            service.ClassUnderTest.Compile(new[] { context, context2 });

            service.Mock<IProcessHelper>().Verify(x => x.RunBatchCompileProcess(It.IsAny<BatchCompileConfiguration>()), Times.Exactly(2));
            Assert.Equal(@"C:\src\a.js", context.ReferencedFiles.ElementAt(0).GeneratedFilePath);
            Assert.Equal(@"C:\src\b.js", context.ReferencedFiles.ElementAt(1).GeneratedFilePath);
            Assert.Equal(@"C:\other\a.js", context2.ReferencedFiles.ElementAt(0).GeneratedFilePath);
            Assert.Equal(@"C:\other\d.js", context2.ReferencedFiles.ElementAt(1).GeneratedFilePath);
        }
    }
}