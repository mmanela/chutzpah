using System.IO;
using Chutzpah.Exceptions;
using Chutzpah.FileGenerator;
using Chutzpah.FileGenerators;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class CoffeeScriptFileGeneratorFacts
    {
        private class TestableCoffeeScriptFileGenerator : Testable<CoffeeScriptFileGenerator>
        {
            public TestableCoffeeScriptFileGenerator()
            {
            }
        }

        public class CanHandleFile
        {
            [Fact]
            public void Will_return_false_for_non_coffee_script_files()
            {
                var converter = new TestableCoffeeScriptFileGenerator();
                var file = new ReferencedFile { Path = "somePath.js" };

                var result = converter.ClassUnderTest.CanHandleFile(file);

                Assert.False(result);
            }

            [Fact]
            public void Will_return_true_for_coffee_script_files()
            {
                var converter = new TestableCoffeeScriptFileGenerator();
                var file = new ReferencedFile { Path = "somePath.coffee" };

                var result = converter.ClassUnderTest.CanHandleFile(file);

                Assert.True(result);
            }
        }


        public class GenerateCompiledSources
        {
            [Fact]
            public void Will_set_the_correct_filename_on_a_compilation_failed_exception()
            {
                var generator = new TestableCoffeeScriptFileGenerator();
                var file1 = new ReferencedFile { Path = "path1.coffee" };
                var file2 = new ReferencedFile { Path = "path2.coffee" };

                generator.Mock<IFileSystemWrapper>().Setup(x => x.GetText("path1.coffee")).Returns("content1");
                generator.Mock<IFileSystemWrapper>().Setup(x => x.GetText("path2.coffee")).Returns("content2");
                
                generator.Mock<ICoffeeScriptEngineWrapper>().Setup(x => x.Compile("content1", It.IsAny<object[]>())).Returns("compiled");
                generator.Mock<ICoffeeScriptEngineWrapper>().Setup(x => x.Compile("content2", It.IsAny<object[]>())).
                    Throws(new ChutzpahCompilationFailedException("Parse error"));

                generator.Mock<ICompilerCache>().Setup(x => x.Get(It.IsAny<string>())).Returns((string)null);

                var ex = Record.Exception(() => generator.ClassUnderTest.GenerateCompiledSources(new[] { file1, file2 },
                    new ChutzpahTestSettingsFile().InheritFromDefault())) as ChutzpahCompilationFailedException;

                Assert.Equal("path2.coffee", ex.SourceFile);
            }

            [Fact]
            public void Will_return_compiled_files_and_set_to_cache()
            {
                var generator = new TestableCoffeeScriptFileGenerator();
                var file = new ReferencedFile { Path = "path1.ts" };
                generator.Mock<ICoffeeScriptEngineWrapper>().Setup(x => x.Compile(It.IsAny<string>(), It.IsAny<object[]>())).Returns("compiled");
                generator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns("content");
                generator.Mock<ICompilerCache>().Setup(x => x.Get(It.IsAny<string>())).Returns((string)null);

                var result = generator.ClassUnderTest.GenerateCompiledSources(new[] { file }, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal("compiled", result[file.Path]);
                generator.Mock<ICompilerCache>().Verify(x => x.Set("coffeeScriptBareMode:True,Source:content", "compiled"));
            }

            [Fact]
            public void Will_return_compiled_files_that_are_cache()
            {
                var generator = new TestableCoffeeScriptFileGenerator();
                var file = new ReferencedFile { Path = "path1.ts" };
                generator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns("content");
                generator.Mock<ICompilerCache>().Setup(x => x.Get(It.IsAny<string>())).Returns("compiled");

                var result = generator.ClassUnderTest.GenerateCompiledSources(new[] { file }, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal("compiled", result[file.Path]);
                generator.Mock<ICoffeeScriptEngineWrapper>().Verify(x => x.Compile(It.IsAny<string>()), Times.Never());
                generator.Mock<ICompilerCache>().Verify(x => x.Set(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            }
        }
    }
}