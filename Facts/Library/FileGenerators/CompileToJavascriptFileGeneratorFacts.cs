using System.Collections.Generic;
using Chutzpah.FileGenerator;
using Chutzpah.FileGenerators;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Xunit;

namespace Chutzpah.Facts
{
    public class CompileToJavascriptFileGeneratorFacts
    {
        private class TestableFileGenerator : CompileToJavascriptFileGenerator
        {
            public bool CanHandle { get; set; }

            public TestableFileGenerator(IFileSystemWrapper fileSystem, ICompilerEngineWrapper compilerEngineWrapper, ICompilerCache compilerCache)
                : base(fileSystem, compilerEngineWrapper,compilerCache)
            {
            }

            public override bool CanHandleFile(ReferencedFile referencedFile)
            {
                return CanHandle;
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

                generator.ClassUnderTest.Generate(file, tempFiles);

                Assert.Equal("somePath.js", file.Path);
                Assert.Empty(tempFiles);
            }

            [Fact]
            public void Will_convert_to_js_file_and_update_generated_path_to_new_file()
            {
                var generator = new TestableCompileToJavascriptFileGenerator();
                generator.ClassUnderTest.CanHandle = true;
                var file = new ReferencedFile { Path = @"path\to\someFile.coffee" };
                generator.Mock<ICompilerEngineWrapper>().Setup(x => x.Compile("coffeeContents")).Returns("jsContents");
                generator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\to\someFile.coffee")).Returns("coffeeContents");
                var resultPath = @"path\to\" + string.Format(Constants.ChutzpahTemporaryFileFormat, "someFile.js");
                var tempFiles = new List<string>();

                generator.ClassUnderTest.Generate(file, tempFiles);

                generator.Mock<IFileSystemWrapper>().Verify(x => x.WriteAllText(resultPath, "jsContents"));
                Assert.Equal(@"path\to\someFile.coffee", file.Path);
                Assert.Equal(resultPath, file.GeneratedFilePath);
                Assert.Contains(resultPath, tempFiles);
            }

        }
    }
}