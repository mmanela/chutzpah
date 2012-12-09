using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public void Will_test_if_file_exits_when_created()
        {
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
        public void Will_try_to_open_file_if_it_exists()
        {
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(true);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(new MemoryStream());

            var result = cache.ClassUnderTest.Get("Coffee");

            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Once());
        }

        [Fact]
        public void Will_overwrite_a_non_cachefile()
        {
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(true);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(new MemoryStream());
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(new MemoryStream());

            cache.ClassUnderTest.Set("coffeescript/typescript", "javascript");
            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>().Verify(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
        }

        [Fact]
        public void Will_save_cachefile()
        {
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(new MemoryStream());

            cache.ClassUnderTest.Set("coffeescript/typescript", "javascript");
            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
        }

        [Fact]
        public void Will_cache_a_value()
        {
            var cache = new Testable<CompilerCache>();
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);

            cache.ClassUnderTest.Set("coffeescript/typescript", "javascript");

            var result = cache.ClassUnderTest.Get("coffeescript/typescript");
            Assert.Equal("javascript",result);
        }

        [Fact]
        public void Will_limit_the_size_of_the_cache_file()
        {
            var cache = new Testable<CompilerCache>();

            var ms = new ExplicitDisposableMemoryStream(10 * 1024 * 1024);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(ms);

            
            // Fill cache with 12MB
            for (var i = 0; i < 12288; i++)
            {
                var kiloByte = new String('x', 1024);
                cache.ClassUnderTest.Set(i.ToString(), kiloByte);
            }
            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
            Assert.InRange(ms.Position,8000000,8388608);
            ms.ExplicitDispose();
        }

        [Fact]
        public void Will_keep_new_values_and_discard_old_when_limiting()
        {
            var firstCache = new Testable<CompilerCache>();

            var ms = new ExplicitDisposableMemoryStream(10 * 1024 * 1024);
            firstCache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            firstCache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            firstCache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(ms);

            firstCache.ClassUnderTest.Set("ShouldBeRemoved", "SomeCode");
            System.Threading.Thread.Sleep(500); 
            // Fill cache with 12MB
            for (var i = 0; i < 12288; i++)
            {
                var kiloByte = new String('x', 1024);
                firstCache.ClassUnderTest.Set(i.ToString(), kiloByte);
                System.Threading.Thread.Sleep(1); 
            }
            System.Threading.Thread.Sleep(500); 
            firstCache.ClassUnderTest.Set("ShouldStillBeHere", "SomeOtherCode");
            firstCache.ClassUnderTest.Save();
            
            ms.SetLength(ms.Position);
            ms.Position = 0;
            var secondCache = new Testable<CompilerCache>();
            secondCache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            secondCache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(true);
            secondCache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(ms);

            var savedValue = secondCache.ClassUnderTest.Get("ShouldStillBeHere");
            var removedValue = secondCache.ClassUnderTest.Get("ShouldBeRemoved");

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