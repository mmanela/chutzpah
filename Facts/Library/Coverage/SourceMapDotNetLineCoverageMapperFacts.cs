using Chutzpah.Coverage;
using Chutzpah.Wrappers;
using Moq;
using SourceMapDotNet;
using SourceMapDotNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;
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

            Assert.Throws(typeof(ArgumentNullException), () => mapper.GetOriginalFileLineExecutionCounts(null, 1, new ReferencedFile()));
        }

        [Fact] public void GetOriginalFileLineExecutionCounts_ThrowsArgumentNullException_IfNoReferencedFileSupplied()
        {
            var mapper = new TestableSourceMapDotNetLineCoverageMapper(GetFakeMappings());

            Assert.Throws(typeof(ArgumentNullException), () => mapper.GetOriginalFileLineExecutionCounts(new int?[0], 1, null));
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ThrowsArgumentException_IfMapFileDoesNotExist()
        {
            var sourceMapFilePath = "path";

            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.FileExists(sourceMapFilePath))
                .Returns(false);

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(GetFakeMappings());

            Assert.Throws(typeof(ArgumentException), () => mapper.GetOriginalFileLineExecutionCounts(new int?[0], 1, new ReferencedFile() { SourceMapFilePath = sourceMapFilePath }));
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ReturnsInputLineExecutionCounts_IfNoSourceMapSpecified()
        {
            var mapper = new TestableSourceMapDotNetLineCoverageMapper(GetFakeMappings());
            var counts = new int?[1];

            var result = mapper.GetOriginalFileLineExecutionCounts(counts, 1, new ReferencedFile());
            Assert.Same(result, counts);
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_ReadsSourceMapFromDisk()
        {
            var sourceMapFilePath = @"C:\folder\with spaces\path";

            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.GetText(sourceMapFilePath))
                .Returns("contents");
            mockFileSystem
                .Setup(x => x.FileExists(sourceMapFilePath))
                .Returns(true);

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(mockFileSystem.Object, GetFakeMappings());
            var result = mapper.GetOriginalFileLineExecutionCounts(new int?[] { null, 1 }, 3, new ReferencedFile() { SourceMapFilePath = sourceMapFilePath, Path = sourceMapFilePath });

            mockFileSystem.Verify(x => x.GetText(sourceMapFilePath), Times.Once());
            Assert.Equal("contents", mapper.LastConsumerFileContents);
        }
        
        [Fact]
        public void GetOriginalFileLineExecutionCounts_AggregatesLineExecutionCountsViaMapping()
        {
            var sourceMapFilePath = @"C:\folder\with spaces\path";

            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.GetText(sourceMapFilePath))
                .Returns("contents");
            mockFileSystem
                .Setup(x => x.FileExists(sourceMapFilePath))
                .Returns(true);
            mockFileSystem
                .Setup(x => x.GetFileName("source.file"))
                .Returns("source.file");

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(mockFileSystem.Object, GetFakeMappings());
            // Line 1 executed twice, line 2 once, line 3 never, line 4 never
            var result = mapper.GetOriginalFileLineExecutionCounts(new int?[] { null, 2, 1, null, null }, 3, new ReferencedFile() { Path  = @"C:\folder\with spaces\source.file", SourceMapFilePath = sourceMapFilePath });

            var expected = new int?[] 
            {
                null,
                2,
                2,
                null
            };

            Assert.Equal(expected.Length, result.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }

        [Fact]
        public void GetOriginalFileLineExecutionCounts_AggregatesLineExecutionCountsViaMapping_ExcludesOtherFilesReferencedInSourceMap()
        {
            var sourceMapFilePath = @"C:\folder\with spaces\path";

            var mockFileSystem = new Mock<IFileSystemWrapper>();
            mockFileSystem
                .Setup(x => x.GetText(sourceMapFilePath))
                .Returns("{ file: 'other.file' }");
            mockFileSystem
                .Setup(x => x.FileExists(sourceMapFilePath))
                .Returns(true);
            mockFileSystem
                .Setup(x => x.GetFileName("other.file"))
                .Returns("other.file");

            var mapper = new TestableSourceMapDotNetLineCoverageMapper(mockFileSystem.Object, GetFakeMappings());
            // Line 1 executed never, line 2 never, line 3 once, line 4 never
            var result = mapper.GetOriginalFileLineExecutionCounts(new int?[] { null, 2, 1, null, null }, 3, new ReferencedFile() { Path  = @"C:\folder\with spaces\other.file", SourceMapFilePath = sourceMapFilePath });

            var expected = new int?[] 
            {
                null,
                null,
                1,
                null
            };

            Assert.Equal(expected.Length, result.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], result[i]);
            }
        }

        private class FileLineMapping
        {
            public string File;
            public int LineNumber;

            public FileLineMapping(int lineNumber, string file)
            {
                this.LineNumber = lineNumber;
                this.File = file;
            }
        }

        private IDictionary<int, FileLineMapping[]> GetFakeMappings()
        {
            return new Dictionary<int, FileLineMapping[]> 
            {
                { 1, new[] { new FileLineMapping(1, "source.file"), new FileLineMapping(2, "source.file") } },
                { 2, new[] { new FileLineMapping(2, "other.file") } },
                { 3, new FileLineMapping[0] },
                { 4, new[] { new FileLineMapping(3, "source.file") } }
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

            public TestableSourceMapDotNetLineCoverageMapper(IFileSystemWrapper fileSystem, IDictionary<int, FileLineMapping[]> mappings)
                : base(fileSystem)
            {
                this.mappings = mappings.ToDictionary(x => x.Key, x => x.Value.Select(y => new SourceReference { LineNumber = y.LineNumber, File = y.File }).ToArray());
            }

            public TestableSourceMapDotNetLineCoverageMapper(IDictionary<int, FileLineMapping[]> mappings)
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
