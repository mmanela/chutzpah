using Chutzpah.Coverage;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Chutzpah.Facts.Library.Coverage
{
    public class BlanketJsCoverageEngineFacts
    {
        class TestableCoverageEngine : Testable<BlanketJsCoverageEngine>
        {
            IDictionary<string, int?[]> coverage;

            public TestableCoverageEngine(IDictionary<string, int?[]> coverage, int?[] mapperOutput = null)
            {
                this.coverage = coverage;

                var coverageObject = new BlanketJsCoverageEngine.BlanketCoverageObject();
                foreach (var kvp in coverage)
                {
                    coverageObject[kvp.Key] = kvp.Value;

                }

                Mock<IJsonSerializer>()
                    .Setup(x => x.Deserialize<BlanketJsCoverageEngine.BlanketCoverageObject>("the json"))
                    .Returns(coverageObject);

                Mock<ILineCoverageMapper>()
                    .Setup(x => x.GetOriginalFileLineExecutionCounts(It.IsAny<int?[]>(), It.IsAny<int>(), It.IsAny<ReferencedFile>()))
                    .Returns(mapperOutput ?? new int?[0]);

                Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetLines(@"X:\file1.js"))
                    .Returns(new[] { "javascript" });

                Mock<IFileSystemWrapper>()
                    .Setup(x => x.GetLines(@"X:\file1.ts"))
                    .Returns(new[] { "typescript" });



                Mock<IFileSystemWrapper>()
                    .Setup(x => x.FileExists(It.IsAny<string>()))
                    .Returns(true);
            }
        }

        private static IDictionary<string, int?[]> GetLineExecutions()
        {
            return new Dictionary<string, int?[]>
            {
                { @"X:\file1.js", new int?[]{ 1, 2, null, 4 } }
            };
        }

        private static TestContext GetContext()
        {
            var context =  new TestContext
            {
                TestFileSettings = new ChutzpahTestSettingsFile
                {
                    Compile = new BatchCompileConfiguration
                    {
                        UseSourceMaps = true,
                    }
                }.InheritFromDefault(),
                ReferencedFiles = new[]
                {
                    new ReferencedFile { Path = @"X:\file1.ts", GeneratedFilePath = @"X:\file1.js", SourceMapFilePath = @"X:\file1.map" }
                }
            };

            context.TestFileSettings.Server = null;

            return context;
        }

        [Fact]
        public void DeserializeCoverageObject_UsesLineCoverageMapper_IfSettingsEnabledAndMapPathNotNull()
        {
            var testContext = GetContext();

            var underTest = new TestableCoverageEngine(GetLineExecutions());
            underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            underTest.Mock<ILineCoverageMapper>().Verify(x => x.GetOriginalFileLineExecutionCounts(It.IsAny<int?[]>(), It.IsAny<int>(), It.IsAny<ReferencedFile>()), Times.Once());
        }

        [Fact]
        public void DeserializeCoverageObject_CallsLineCoverageMapper_WithAppropriateArguments()
        {
            var testContext = GetContext();
            var coverageDict = GetLineExecutions();
            var underTest = new TestableCoverageEngine(coverageDict);
            underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            var file = testContext.ReferencedFiles.Single(x => x.Path == @"X:\file1.ts");

            underTest.Mock<ILineCoverageMapper>().Verify(x => x.GetOriginalFileLineExecutionCounts(coverageDict[file.GeneratedFilePath], 1, file), Times.Once());
        }

        [Fact]
        public void DeserializeCoverageObject_UsesOriginalSource_WhenSourceMapsEnabled()
        {
            var testContext = GetContext();
            var coverageDict = GetLineExecutions();
            var mapperOutput = new int?[] { 1, null };
            var underTest = new TestableCoverageEngine(coverageDict, mapperOutput);
            var file = testContext.ReferencedFiles.Single(x => x.Path == @"X:\file1.ts");

            var result = underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            Assert.True(result.ContainsKey(@"X:\file1.ts"));
            Assert.Same(mapperOutput, result[@"X:\file1.ts"].LineExecutionCounts);
            Assert.True(result[@"X:\file1.ts"].SourceLines.All(x => x == "typescript"));
            Assert.Equal(@"X:\file1.ts", result[@"X:\file1.ts"].FilePath);
        }

        [Fact]
        public void DeserializeCoverageObject_UsesGeneratedSource_IfNoOriginalSourceAvailable()
        {
            var testContext = GetContext();
            var coverageDict = GetLineExecutions();

            // Add in a file we didn't know about
            coverageDict[@"X:\file3.js"] = new int?[0];

            var mapperOutput = new int?[] { 1, null };
            var underTest = new TestableCoverageEngine(coverageDict, mapperOutput);
            var file = testContext.ReferencedFiles.Single(x => x.Path == @"X:\file1.ts");

            var result = underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            Assert.True(result.ContainsKey(@"X:\file3.js"));
        }

        [Fact]
        public void DeserializeCoverageObject_UsesGeneratedSource_WhenSourceMapsDisabled()
        {
            var testContext = GetContext();
            testContext.TestFileSettings.Compile.UseSourceMaps = false;
            var coverageDict = GetLineExecutions();
            var mapperOutput = new int?[] { 1, null };
            var underTest = new TestableCoverageEngine(coverageDict, mapperOutput);
            var file = testContext.ReferencedFiles.Single(x => x.Path == @"X:\file1.ts");

            var result = underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            Assert.True(result.ContainsKey(@"X:\file1.ts"));
            Assert.Same(coverageDict[@"X:\file1.js"], result[@"X:\file1.ts"].LineExecutionCounts);
            Assert.True(result[@"X:\file1.ts"].SourceLines.All(x => x == "javascript"));
            Assert.Equal(@"X:\file1.ts", result[@"X:\file1.ts"].FilePath);
        }

        [Fact]
        public void DeserializeCoverageObject_SkipsLineCoverageMapper_IfSettingsDisabled()
        {
            var testContext = GetContext();
            testContext.TestFileSettings.Compile.UseSourceMaps = false;

            var underTest = new TestableCoverageEngine(GetLineExecutions());
            underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            underTest.Mock<ILineCoverageMapper>().Verify(x => x.GetOriginalFileLineExecutionCounts(It.IsAny<int?[]>(), It.IsAny<int>(), It.IsAny<ReferencedFile>()), Times.Never());
        }

        [Fact]
        public void DeserializeCoverageObject_SkipsLineCoverageMapper_IfMapPathNull()
        {
            var testContext = GetContext();
            foreach (var file in testContext.ReferencedFiles)
            {
                file.SourceMapFilePath = null;
            }

            var underTest = new TestableCoverageEngine(GetLineExecutions());
            underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);

            underTest.Mock<ILineCoverageMapper>().Verify(x => x.GetOriginalFileLineExecutionCounts(It.IsAny<int?[]>(), It.IsAny<int>(), It.IsAny<ReferencedFile>()), Times.Never());
        }


        [Fact(Skip ="For now")]
        public void DeserializeCoverageObject_NonCanonicalReferences_MergeCoverage()
        {
            var testContext = new TestContext
            {
                TestFileSettings = new ChutzpahTestSettingsFile
                {
                    Compile = new BatchCompileConfiguration
                    {
                        UseSourceMaps = false,
                    }
                }.InheritFromDefault(),
                ReferencedFiles = new[]
               {
                    new ReferencedFile
                        {
                            Path = @"X:\file1.ts", GeneratedFilePath = @"X:\file1.js"
                        },

                    new ReferencedFile
                        {
                            Path = @"X:\1\..\file1.ts", GeneratedFilePath = @"X:\1\..\file1.js"
                        },
                }
            };
            var lineExecutions =  new Dictionary<string, int?[]>
            {
                { @"X:\file1.js", new int?[]{ null, null, 4, 4 } },
                { @"X:\1\..\file1.js", new int?[]{ 1, 2, null, null } }
            };

            var underTest = new TestableCoverageEngine(lineExecutions);
            var coverage = underTest.ClassUnderTest.DeserializeCoverageObject("the json", testContext);
            
            Assert.Equal(1, coverage.CoveragePercentage, 2);
        }
    }
}
