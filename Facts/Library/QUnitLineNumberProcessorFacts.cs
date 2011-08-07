using Chutzpah.Wrappers;
using Moq;
using Xunit;
using Chutzpah.FileProcessors;
using Chutzpah.Models;

namespace Chutzpah.Facts
{
    public class QUnitLineNumberProcessorFacts
    {
        private class TestableQUnitLineNumberProcessor : QUnitLineNumberProcessor
        {
            public Mock<IFileSystemWrapper> MoqFileSystemWrapper { get; set; }

            private TestableQUnitLineNumberProcessor(Mock<IFileSystemWrapper> moqFileSystemWrapper)
                : base(moqFileSystemWrapper.Object)
            {
                MoqFileSystemWrapper = moqFileSystemWrapper;
            }


            public static TestableQUnitLineNumberProcessor Create()
            {
                var prob = new TestableQUnitLineNumberProcessor(new Mock<IFileSystemWrapper>());

                return prob;
            }
        }

        public class Process
        {

            [Fact]
            public void Will_get_skip_if_file_is_not_under_test()
            {
                var processor = TestableQUnitLineNumberProcessor.Create();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = false, StagedPath = "path" };

                processor.Process(file);

                processor.MoqFileSystemWrapper.Verify(x => x.GetLines(It.IsAny<string>()), Times.Never());
            }

            [Fact]
            public void Will_get_line_number_for_tests()
            {
                var processor = TestableQUnitLineNumberProcessor.Create();
                var file = new ReferencedFile { IsLocal = true, IsFileUnderTest = true, StagedPath = "path" };
                processor.MoqFileSystemWrapper.Setup(x => x.GetLines("path")).Returns(new string[] 
                {
                    "//js file", "test (\"test1\", function(){}); ", "module ( \"module1\");", "  test('test2', function(){});"
                });

                processor.Process(file);

                Assert.Equal(2, file.FilePositions.Get("","test1").Line);
                Assert.Equal(1, file.FilePositions.Get("","test1").Column);
                Assert.Equal(4, file.FilePositions.Get("module1","test2").Line);
                Assert.Equal(3, file.FilePositions.Get("module1","test2").Column);
            }

        }
    }
}