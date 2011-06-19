using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class FileProbeFacts
    {
        private class TestableFileProbe : FileProbe
        {
            public Mock<IFileSystemWrapper> MoqFileSystemWrapper { get; set; }
            public Mock<IEnvironmentWrapper> MoqEnvironmentWrapper { get; set; }

            private TestableFileProbe(Mock<IEnvironmentWrapper> moqEnvironmentWrapper, Mock<IFileSystemWrapper> moqFileSystemWrapper)
                : base(moqEnvironmentWrapper.Object, moqFileSystemWrapper.Object)
            {
                MoqFileSystemWrapper = moqFileSystemWrapper;
                MoqEnvironmentWrapper = moqEnvironmentWrapper;
            }


            public static TestableFileProbe Create()
            {
                var prob = new TestableFileProbe(new Mock<IEnvironmentWrapper>(), new Mock<IFileSystemWrapper>());
                prob.MoqFileSystemWrapper.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns("someDirName");
                return prob;
            }
        }

        public class FindPath
        {
            [Fact]
            public void Will_return_null_if_fileName_is_null_or_empty()
            {
                var prob = TestableFileProbe.Create();

                var path = prob.FindPath(null);

                Assert.Null(path);
            }

            [Fact]
            public void Will_return_full_path_of_fileName_in_executing_if_file_exists()
            {
                var prob = TestableFileProbe.Create();
                prob.MoqEnvironmentWrapper.Setup(x => x.GetExeuctingAssemblyPath()).Returns(@"c:\dir\thing.exe");
                prob.MoqFileSystemWrapper.Setup(x => x.FileExists(@"c:\dir\path.html")).Returns(true);
                prob.MoqFileSystemWrapper.Setup(x => x.GetDirectoryName(@"c:\dir\thing.exe")).Returns(@"c:\dir");
                
                var path = prob.FindPath("path.html");

                Assert.Equal(@"c:\dir\path.html", path);
            }


            [Fact]
            public void Will_return_full_path_of_fileName_in_current_directory_if_file_exists()
            {
                var prob = TestableFileProbe.Create();
                prob.MoqFileSystemWrapper.Setup(x => x.GetFullPath(@"path.html")).Returns(@"d:\other\path.html");
                prob.MoqFileSystemWrapper.Setup(x => x.FileExists(@"d:\other\path.html")).Returns(true);


                var path = prob.FindPath("path.html");

                Assert.Equal(@"d:\other\path.html", path);
            }

            [Fact]
            public void Will_return_null_if_all_attempts_fail()
            {
                var prob = TestableFileProbe.Create();

                var path = prob.FindPath("somePath");

                Assert.Null(path);
            }
        }
    }
}