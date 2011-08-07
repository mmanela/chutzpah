using Chutzpah.Wrappers;
using Moq;
using Xunit;
using StructureMap.AutoMocking;

namespace Chutzpah.Facts
{
    public class FileProbeFacts
    {
        private class TestableFileProbe : Testable<FileProbe>
        {
            public TestableFileProbe()
            {
                Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns("someDirName");
            }
        }

        public class FindPath
        {
            [Fact]
            public void Will_return_null_if_fileName_is_null_or_empty()
            {
                var prob = new TestableFileProbe();

                var path = prob.ClassUnderTest.FindPath(null);

                Assert.Null(path);
            }

            [Fact]
            public void Will_return_full_path_of_fileName_in_executing_if_file_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IEnvironmentWrapper>().Setup(x => x.GetExeuctingAssemblyPath()).Returns(@"c:\dir\thing.exe");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"c:\dir\path.html")).Returns(true);
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetDirectoryName(@"c:\dir\thing.exe")).Returns(@"c:\dir");
                
                var path = prob.ClassUnderTest.FindPath("path.html");

                Assert.Equal(@"c:\dir\path.html", path);
            }


            [Fact]
            public void Will_return_full_path_of_fileName_in_current_directory_if_file_exists()
            {
                var prob = new TestableFileProbe();
                prob.Mock<IFileSystemWrapper>().Setup(x => x.GetFullPath(@"path.html")).Returns(@"d:\other\path.html");
                prob.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(@"d:\other\path.html")).Returns(true);


                var path = prob.ClassUnderTest.FindPath("path.html");

                Assert.Equal(@"d:\other\path.html", path);
            }

            [Fact]
            public void Will_return_null_if_all_attempts_fail()
            {
                var prob = new TestableFileProbe();

                var path = prob.ClassUnderTest.FindPath("somePath");

                Assert.Null(path);
            }
        }
    }
}