namespace Chutzpah.Facts.Library
{
    using Chutzpah.FileProcessors;
    using Chutzpah.FrameworkDefinitions;
    using Chutzpah.Models;
    using Chutzpah.Wrappers;
    using Moq;
    using Xunit;

    public class JasmineLineNumberProcessorFacts
    {
        private class TestableJasmineLineNumberProcessor : Testable<JasmineLineNumberProcessor>
        {
            public TestableJasmineLineNumberProcessor()
            {
            }
        }

        public class Process
        {
            [Fact]
            public void Will_get_skip_if_file_is_not_under_test()
            {
                var processor = new TestableJasmineLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = false, Path = "path" };

                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor.Mock<IFileSystemWrapper>().Verify(x => x.GetLines(It.IsAny<string>()), Times.Never());
            }

            [Fact]
            public void Will_get_line_number_for_tests()
            {
                var processor = new TestableJasmineLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, Path = "path" };
                var text =
@"//js file
describe ('module1', function(){
  it('test1', function(){});
    it('test2', function(){});
});";


                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, text, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal(3, file.FilePositions[0].Line);
                Assert.Equal(7, file.FilePositions[0].Column);
                Assert.Equal(4, file.FilePositions[1].Line);
                Assert.Equal(9, file.FilePositions[1].Column);
            }

            [Fact]
            public void Will_get_line_number_for_tests_in_CoffeeScript_file()
            {
                var processor = new TestableJasmineLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, Path = "path.coffee" };
                var text =
@"//CoffeeScript file
describe 'module1', ->;
  it 'test1', ->";

                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, text, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal(3, file.FilePositions[0].Line);
                Assert.Equal(7, file.FilePositions[0].Column);
            }

            [Fact]
            public void Will_get_line_number_for_test_with_quotes_in_title()
            {
                var processor = new TestableJasmineLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, Path = "path" };
                var text =
@"describe ( 'modu""le\'1', function () {
 it ('t""e\'st1', function(){});
};";


                processor.ClassUnderTest.Process(new Mock<IFrameworkDefinition>().Object, file, text, new ChutzpahTestSettingsFile().InheritFromDefault());

                Assert.Equal(2, file.FilePositions[0].Line);
                Assert.Equal(7, file.FilePositions[0].Column);
            }
        }
    }
}