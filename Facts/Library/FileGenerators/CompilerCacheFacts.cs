using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Chutzpah.Utility;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class CompilerCacheFacts
    {
        
        [Fact]
        public void Will_test_if_default_file_exists_when_instantiated()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(new MemoryStream());

            var result = cache.ClassUnderTest.Get("Coffee");

            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Never());
        }

        [Fact]
        public void Will_test_if_cachefile_by_commandline_exists_when_instantiated()
        {
            GlobalOptions.Instance.CompilerCacheFile = "my_cache.dat";
            var cache = new Testable<CompilerCache>();
            
            var cacheFile = "my_cache.dat";
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(new MemoryStream());

            var result = cache.ClassUnderTest.Get("Coffee");

            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Never());
        }

        [Fact]
        public void Will_try_to_open_default_file_if_it_exists()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(true);
            cache.Mock<IBinarySerializer>()
                .Setup(x => x.Deserialize <ConcurrentDictionary<string, Tuple<DateTime, string>>>(It.IsAny<Stream>()))
                .Returns(new ConcurrentDictionary<string, Tuple<DateTime, string>>());

            var result = cache.ClassUnderTest.Get("Coffee");

            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Once());
        }

        [Fact]
        public void Will_try_to_open_cachefile_by_commandline_if_it_exists()
        {
            GlobalOptions.Instance.CompilerCacheFile = "my_cache.dat";
            var cache = new Testable<CompilerCache>();

            var cacheFile = "my_cache.dat";
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(true);
            cache.Mock<IBinarySerializer>()
                .Setup(x => x.Deserialize <ConcurrentDictionary<string, Tuple<DateTime, string>>>(It.IsAny<Stream>()))
                .Returns(new ConcurrentDictionary<string, Tuple<DateTime, string>>());

            var result = cache.ClassUnderTest.Get("Coffee");

            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Once());
        }

        [Fact]
        public void Will_overwrite_the_file_even_if_it_isnt_a_cachefile()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(true);
            cache.Mock<IBinarySerializer>()
                .Setup(x => x.Deserialize<ConcurrentDictionary<string, Tuple<DateTime, string>>>(It.IsAny<Stream>()))
                .Returns(new ConcurrentDictionary<string, Tuple<DateTime, string>>());
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(new MemoryStream());


            var result = cache.ClassUnderTest.Get("Coffee");

            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
        }

        [Fact]
        public void Will_save_cachefile()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(new MemoryStream());
            
            var result = cache.ClassUnderTest.Get("Coffee");

            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
        }

        [Fact]
        public void Will_cache_a_value()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);

            cache.ClassUnderTest.Set("Coffee", "javascript");
            var result = cache.ClassUnderTest.Get("Coffee");

            Assert.Equal("javascript",result);
        }

        [Fact]
        public void Will_limit_the_size_of_the_cache_file()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes = 100;
            var cache = new Testable<CompilerCache>();

            var ms = new ExplicitDisposableMemoryStream(GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes * 2);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(ms);


            cache.ClassUnderTest.Set("buffer", new string('x', 100));
            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
            Assert.True(ms.Position < GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes);
            ms.ExplicitDispose();
        }

        [Fact]
        public void Will_keep_new_values_and_discard_old_when_limiting()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes = 100;
            var cache = new Testable<CompilerCache>();

            var ms = new ExplicitDisposableMemoryStream(GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes * 2);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);

            cache.ClassUnderTest.Set("ShouldBeRemoved", "SomeCode");
            Thread.Sleep(5);
            // Fill cache with more than the max-size
            cache.ClassUnderTest.Set("buffer", new string('x',100));
            Thread.Sleep(5);
            cache.ClassUnderTest.Set("ShouldStillBeHere", "SomeOtherCode");
            cache.ClassUnderTest.Save();

            var savedValue = cache.ClassUnderTest.Get("ShouldStillBeHere");
            var removedValue = cache.ClassUnderTest.Get("ShouldBeRemoved");

            Assert.True(string.IsNullOrEmpty(removedValue));
            Assert.Equal("SomeOtherCode",savedValue);

            ms.ExplicitDispose();

        }
    }

    internal class ExplicitDisposableMemoryStream : MemoryStream
    {

        public ExplicitDisposableMemoryStream(int capacity)
            : base(capacity)
        {

        }

        protected override void Dispose(bool disposing)
        {

        }
        public override void Close()
        {
            // Do not close. We need to be able to query the stream.
        }

        public void ExplicitDispose()
        {
            base.Close();
            Dispose();
        }
    }
}