namespace Chutzpah.Facts.Library
{
    using Chutzpah.FileProcessors;
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
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = false, StagedPath = "path" };

                processor.ClassUnderTest.Process(file);

                processor.Mock<IFileSystemWrapper>().Verify(x => x.GetLines(It.IsAny<string>()), Times.Never());
            }

            [Fact]
            public void Will_get_line_number_for_tests()
            {
                var processor = new TestableJasmineLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, StagedPath = "path" };
                processor.Mock<IFileSystemWrapper>().Setup(x => x.GetLines("path")).Returns(new string[] 
                {
                    "//js file",
                    "describe ('module1', function(){",
                    "  it('test1', function(){});",
                    "    it('test2', function(){});",
                    "});"
                });

                processor.ClassUnderTest.Process(file);

                Assert.Equal(3, file.FilePositions[0].Line);
                Assert.Equal(3, file.FilePositions[0].Column);
                Assert.Equal(4, file.FilePositions[1].Line);
                Assert.Equal(5, file.FilePositions[1].Column);
            }

            [Fact]
            public void Will_get_line_number_for_test_with_quotes_in_title()
            {
                var processor = new TestableJasmineLineNumberProcessor();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, StagedPath = "path" };
                processor.Mock<IFileSystemWrapper>().Setup(x => x.GetLines("path")).Returns(new string[] 
                {
                    "describe ( 'modu\"le\\'1', function () {",
                    " it (\'t\"e\\'st1\', function(){}); ",
                    "};"
                });

                processor.ClassUnderTest.Process(file);

                Assert.Equal(2, file.FilePositions[0].Line);
                Assert.Equal(2, file.FilePositions[0].Column);
            }
        }
    }
}