using Chutzpah.FileGenerator;
using Chutzpah.FileGenerators;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TypeScriptFileGeneratorFacts
    {
        private class TestableTypeScriptFileGenerator : Testable<TypeScriptFileGenerator>
        {
            public TestableTypeScriptFileGenerator()
            {
                Inject<IJsonSerializer>(new JsonSerializer());
                Mock<IFileSystemWrapper>().Setup(x => x.GetFileName(It.IsAny<string>())).Returns<string>(x => x);
            }
        }

        public class CanHandleFile
        {
            [Fact]
            public void Will_return_false_for_non_typescript_script_files()
            {
                var converter = new TestableTypeScriptFileGenerator();
                var file = new ReferencedFile { Path = "somePath.js" };

                var result = converter.ClassUnderTest.CanHandleFile(file);

                Assert.False(result);
            }

            [Fact]
            public void Will_return_true_for_typescript_script_files()
            {
                var converter = new TestableTypeScriptFileGenerator();
                var file = new ReferencedFile { Path = "somePath.ts" };

                var result = converter.ClassUnderTest.CanHandleFile(file);

                Assert.True(result);
            }
        }

        public class GenerateCompiledSources
        {
            [Fact]
            public void Will_return_compiled_files()
            {
                var generator = new TestableTypeScriptFileGenerator();
                var file = new ReferencedFile { Path = "path1.ts" };
                generator.Mock<ITypeScriptEngineWrapper>().Setup(x => x.Compile(It.IsAny<string>(), It.IsAny<object[]>())).Returns("{\"path1.ts\" :\"compiled\"");
                generator.Mock<IFileSystemWrapper>().Setup(x => x.GetText(It.IsAny<string>())).Returns("content");
                generator.Mock<ITypeScriptEngineWrapper>().Setup(x => x.Compile(It.IsAny<string>())).Returns((string)null);

                var result = generator.ClassUnderTest.GenerateCompiledSources(new[] {file}, new ChutzpahTestSettingsFile());

                Assert.Equal("compiled", result[file.Path]);
            }
        }
    }
}