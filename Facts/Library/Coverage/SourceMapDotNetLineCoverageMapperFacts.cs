using Chutzpah.Coverage;
using Chutzpah.Wrappers;
using Moq;
using SourceMapDotNet;
using SourceMapDotNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Chutzpah.Facts.Library.Coverage
{
    public class SourceMapDotNetLineCoverageMapperFacts
    {
        [Fact]
        public void Constructor_ThrowsArgumentNullException_IfNoIFileSystemSupplied()
        {
            Assert.Throws(typeof(ArgumentNullException), () => new SourceMapDotNetLineCoverageMapper(null));
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ThrowsArgumentNullException_IfNoExecutionCountsSupplied()
        {
            var mapper = new TestableSourceMapDotNetLineCoverageMapper(GetFakeMappings());

            Assert.Throws(typeof(ArgumentNullException), () => mapper.GetOriginalFileLineExecutionCounts(null, 1, "path"));
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ThrowsArgumentException_IfMapFileDoesNotExist()
        {
            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.FileExists("path"))
                .Returns(false);

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(GetFakeMappings());

            Assert.Throws(typeof(ArgumentException), () => mapper.GetOriginalFileLineExecutionCounts(new int?[0], 1, "path"));
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ReturnsInputLineExecutionCounts_IfNoSourceMapSpecified()
        {
            var mapper = new TestableSourceMapDotNetLineCoverageMapper(GetFakeMappings());
            var counts = new int?[1];

            var result = mapper.GetOriginalFileLineExecutionCounts(counts, 1, null);
            Assert.Same(result, counts);
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ReadsSourceMapFromDisk()
        {
            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.GetText("path"))
                .Returns("contents");
            mockFileSystem
                .Setup(x => x.FileExists("path"))
                .Returns(true);

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(mockFileSystem.Object, GetFakeMappings());
            var result = mapper.GetOriginalFileLineExecutionCounts(new int?[]{ null, 1 }, 3, "path");

            mockFileSystem.Verify(x => x.GetText("path"), Times.Once());
            Assert.Equal("contents", mapper.LastConsumerFileContents);
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_AggregatesLineExecutionCountsViaMapping()
        {
            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.GetText("path"))
                .Returns("contents");
            mockFileSystem
                .Setup(x => x.FileExists("path"))
                .Returns(true);

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(mockFileSystem.Object, GetFakeMappings());
            // Line 1 executed twice, line 2 once, line 3 never, line 4 never
            var result = mapper.GetOriginalFileLineExecutionCounts(new int?[] { null, 2, 1, null, null }, 3, "path");

            var expected = new int?[] 
            {
                null,
                2,
                3,
                null
            };

            Assert.Equal(expected.Length, result.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }

        private IDictionary<int, int[]> GetFakeMappings()
        {
            return new Dictionary<int, int[]> 
            {
                { 1, new[] { 1, 2 } },
                { 2, new[] { 2 } },
                { 3, new int[0] },
                { 4, new[] { 3 } }
            };
        }

        class TestableSourceMapDotNetLineCoverageMapper : SourceMapDotNetLineCoverageMapper
        {
            IDictionary<int, SourceReference[]> mappings;

            public string LastConsumerFileContents
            {
                get;
                set;
            }

            public TestableSourceMapDotNetLineCoverageMapper(IFileSystemWrapper fileSystem, IDictionary<int, int[]> mappings)
                : base(fileSystem)
            {
                this.mappings = mappings.ToDictionary(x => x.Key, x => x.Value.Select(y => new SourceReference { LineNumber = y }).ToArray());
            }

            public TestableSourceMapDotNetLineCoverageMapper(IDictionary<int, int[]> mappings)
                : this(new Mock<IFileSystemWrapper>().Object, mappings)
            {
            }

            protected override ISourceMapConsumer GetConsumer(string mapFileContents)
            {
                var toReturn = new Mock<ISourceMapConsumer>();
                toReturn
                    .Setup(x => x.OriginalPositionsFor(It.IsAny<int>()))
                    .Returns((int x) => mappings[x]);

                this.LastConsumerFileContents = mapFileContents;

                return toReturn.Object;
            }
        }
    }
}
