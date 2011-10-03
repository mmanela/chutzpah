using Chutzpah.Wrappers;
using Moq;
using Xunit;
using Chutzpah.Models;
using Chutzpah.TestFileDetectors;
using System;

namespace Chutzpah.Facts
{
    public class QUnitTestableFileDetectorFacts
    {
        private class TestableQUnitTestableFileDetector : Testable<QUnitTestableFileDetector>
        {
            public TestableQUnitTestableFileDetector()
            {
            }
        }

        public class Process
        {
            [Fact]
            public void Will_throw_if_fileName_is_null()
            {
                var detector = new TestableQUnitTestableFileDetector();

                var ex = Record.Exception(() => detector.ClassUnderTest.IsTestableFile(null)) as ArgumentNullException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_return_true_if_test_found()
            {
                var detector = new TestableQUnitTestableFileDetector();
                detector.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText("path"))
                    .Returns(@"//js file
                                test (""test1"", function(){}); 
                                module ( ""module1"");
                                test('test2', function(){});");

                var res = detector.ClassUnderTest.IsTestableFile("path");

                Assert.True(res);
            }

            [Fact]
            public void Will_return_false_if_test_not_found()
            {
                var detector = new TestableQUnitTestableFileDetector();
                detector.Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetText("path"))
                    .Returns(@"//js file
                                NOtAtest (""test1"", function(){}); 
                                module ( ""module1"");
                                a.test('test2', function(){});");

                var res = detector.ClassUnderTest.IsTestableFile("path");

                Assert.False(res);
            }

        }
    }
}