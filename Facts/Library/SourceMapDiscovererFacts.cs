using Chutzpah.FileProcessors;
using Chutzpah.Wrappers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Chutzpah.Facts.Library
{
    public class SourceMapDiscovererFacts
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_IfNullFileSystemWrapperSupplied()
        {
            Assert.Throws(typeof(ArgumentNullException), () => new SourceMapDiscoverer(null));
        }

        [Fact]
        public void FindSourceMaps_ThrowsArgumentNullException_IfNullPathSupplied()
        {
            var wrapper = new Mock<IFileSystemWrapper>();
            var discoverer = new SourceMapDiscoverer(wrapper.Object);

            Assert.Throws(typeof(ArgumentNullException), () => discoverer.FindSourceMap(null));
        }

        [Fact]
        public void FindSourceMaps_ReturnsPath_IfFileWithMapSuffixExists()
        {
            var wrapper = new Mock<IFileSystemWrapper>();
            wrapper
                .Setup(x => x.FileExists("absolute.path.map"))
                .Returns(true);

            var discoverer = new SourceMapDiscoverer(wrapper.Object);
            Assert.Equal("absolute.path.map", discoverer.FindSourceMap("absolute.path"));
        }

        [Fact]
        public void FindSourceMaps_ReturnsNull_IfNoMapFileExists()
        {
            var wrapper = new Mock<IFileSystemWrapper>();
            wrapper
                .Setup(x => x.FileExists("absolute.path.map"))
                .Returns(false);

            var discoverer = new SourceMapDiscoverer(wrapper.Object);
            Assert.Null(discoverer.FindSourceMap("absolute.path"));
        }
    }
}
