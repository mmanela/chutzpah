using System.IO;
using Chutzpah.FileGenerator;
using Chutzpah.FileGenerators;
using Chutzpah.Models;
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
    }
}