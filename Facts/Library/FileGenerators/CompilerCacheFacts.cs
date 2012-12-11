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
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(new MemoryStream());

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
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.Open(cacheFile, FileMode.Open, FileAccess.Read))
                 .Returns(new MemoryStream());

            cache.ClassUnderTest.Get("Coffee");

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
            GlobalOptions.Instance.CompilerCacheFile = null;
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
            GlobalOptions.Instance.CompilerCacheFile = null;
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
            GlobalOptions.Instance.CompilerCacheFile = null;
            var cache = new Testable<CompilerCache>();

            var ms = new ExplicitDisposableMemoryStream(Constants.CompilerCacheFileMaxSize*2);
            cache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            cache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(ms);

            
            // Fill cache with more than the max-size
            for (var i = 0; i < (Constants.CompilerCacheFileMaxSize/1024)+1024; i++)
            {
                var kiloByte = new String('x', 1024);
                cache.ClassUnderTest.Set(i.ToString(), kiloByte);
            }
            cache.ClassUnderTest.Save();
            cache.Mock<IFileSystemWrapper>().Verify(x => x.FileExists(cacheFile), Times.Once());
            cache.Mock<IFileSystemWrapper>()
                 .Verify(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write), Times.Once());
            Assert.InRange(ms.Position, Constants.CompilerCacheFileMaxSize/2, Constants.CompilerCacheFileMaxSize);
            ms.ExplicitDispose();
        }

        [Fact]
        public void Will_keep_new_values_and_discard_old_when_limiting()
        {
            GlobalOptions.Instance.CompilerCacheFile = null;
            var firstCache = new Testable<CompilerCache>();

            var ms = new ExplicitDisposableMemoryStream(Constants.CompilerCacheFileMaxSize*2);
            firstCache.Mock<IFileSystemWrapper>()
                 .Setup(x => x.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder))
                 .Returns("tmp");
            var cacheFile = Path.Combine("tmp", Constants.ChutzpahCompilerCacheFileName);
            firstCache.Mock<IFileSystemWrapper>().Setup(x => x.FileExists(cacheFile)).Returns(false);
            firstCache.Mock<IFileSystemWrapper>().Setup(x => x.Open(cacheFile, FileMode.Create, FileAccess.Write))
                 .Returns(ms);

            firstCache.ClassUnderTest.Set("ShouldBeRemoved", "SomeCode");
            System.Threading.Thread.Sleep(500); 
            // Fill cache with more than the max-size
            for (var i = 0; i < (Constants.CompilerCacheFileMaxSize/1024)+1024; i++)
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