using System.Collections.Generic;
using System.Threading;
using Chutzpah.FileGenerator;
using Chutzpah.FileGenerators;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class CompileToJavascriptFileGeneratorFacts
    {
        private class TestableFileGenerator : CompileToJavascriptFileGenerator
        {
            public bool CanHandle { get; set; }
            public IDictionary<string, string> CompiledSources { get; set; }

            public TestableFileGenerator(IFileSystemWrapper fileSystem)
                : base(fileSystem)
            {
            }

            public override bool CanHandleFile(ReferencedFile referencedFile)
            {
                return CanHandle;
            }


            public override IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles, ChutzpahTestSettingsFile chutzpahTestSettings)
            {
                return CompiledSources;
            }

        }

        private class TestableCompileToJavascriptFileGenerator : Testable<TestableFileGenerator> { }

        public class Generate
        {
            [Fact]
            public void Will_do_nothing_when_file_is_not_supported()
            {
                var generator = new TestableCompileToJavascriptFileGenerator();
                generator.ClassUnderTest.CanHandle = false;
                var file = new ReferencedFile { Path = "somePath.js" };
                var tempFiles = new List<string>();

                generator.ClassUnderTest.Generate(new[] { file }, tempFiles, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal("somePath.js", file.Path);
                Assert.Empty(tempFiles);
            }

            [Fact]
            public void Will_convert_to_js_file_and_update_generated_path_to_new_file()
            {
                var generator = new TestableCompileToJavascriptFileGenerator();
                generator.ClassUnderTest.CanHandle = true;
                generator.ClassUnderTest.CompiledSources = new Dictionary<string, string>
                    {
                        {@"path\to\someFile.coffee", "jsContents"}
                    };
                var file = new ReferencedFile { Path = @"path\to\someFile.coffee" };
                var resultPath = @"path\to\" + string.Format(Constants.ChutzpahTemporaryFileFormat, Thread.CurrentThread.ManagedThreadId, "someFile.js");
                var tempFiles = new List<string>();

                generator.ClassUnderTest.Generate(new[] { file }, tempFiles, new ChutzpahTestSettingsFile().InheritFromDefault());

                generator.Mock<IFileSystemWrapper>().Verify(x => x.WriteAllText(resultPath, "jsContents"));
                Assert.Equal(@"path\to\someFile.coffee", file.Path);
                Assert.Equal(resultPath, file.GeneratedFilePath);
                Assert.Contains(resultPath, tempFiles);
            }
        }
    }
}