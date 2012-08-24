using System.Collections.Generic;
using System.IO;
using Chutzpah.FileConverter;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Xunit;

namespace Chutzpah.Facts
{
    public class CoffeeScriptFileConverterFacts
    {
        private class TestableCoffeeScriptFileConverter : Testable<CoffeeScriptFileConverter>
        {
            public TestableCoffeeScriptFileConverter()
            {
            }
        }

        public class Convert
        {
            [Fact]
            public void Will_do_nothing_to_non_coffee_script_files()
            {
                var converter = new TestableCoffeeScriptFileConverter();
                var file = new ReferencedFile {Path = "somePath.js"};
                var tempFiles = new List<string>();

                converter.ClassUnderTest.Convert(file, tempFiles);

                Assert.Equal("somePath.js", file.Path);
                Assert.Empty(tempFiles);
            }

            [Fact]
            public void Will_convert_to_js_file_and_update_path_if_coffee_file()
            {
                var converter = new TestableCoffeeScriptFileConverter();
                var file = new ReferencedFile { Path = @"path\to\someFile.coffee"};
                converter.Mock<ICoffeeScriptEngineWrapper>().Setup(x => x.Compile("coffeeContents")).Returns("jsContents");
                converter.Mock<IFileSystemWrapper>().Setup(x => x.GetText(@"path\to\someFile.coffee")).Returns("coffeeContents");
                var resultPath = @"path\to\" + string.Format(Constants.ChutzpahTemporaryFileFormat, "someFile.js");
                var tempFiles = new List<string>();

                converter.ClassUnderTest.Convert(file, tempFiles);

                converter.Mock<IFileSystemWrapper>().Verify(x => x.WriteAllText(resultPath, "jsContents"));
                Assert.Equal(resultPath, file.Path);
                Assert.Contains(resultPath, tempFiles);
            }
            
        }
    }
}