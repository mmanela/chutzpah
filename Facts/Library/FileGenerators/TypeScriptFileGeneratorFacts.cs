using Chutzpah.FileGenerator;
using Chutzpah.FileGenerators;
using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts
{
    public class TypeScriptFileGeneratorFacts
    {
        private class TestableTypeScriptFileGenerator : Testable<TypeScriptFileGenerator>
        {
            public TestableTypeScriptFileGenerator()
            {
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
    }
}