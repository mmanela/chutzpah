using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Transformers;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class EmmaXmlTransformerFacts
    {
        private static TestCaseSummary GetTestCaseSummary()
        {
            var testCaseSummary = new TestCaseSummary();
            testCaseSummary.CoverageObject = new CoverageData();

            testCaseSummary.CoverageObject["/no/coverage"] = new CoverageFileData
            {
                FilePath = "/no/coverage",
                LineExecutionCounts = null
            };

            testCaseSummary.CoverageObject["/three/lines/two/covered"] = new CoverageFileData
            {
                FilePath = "/three/lines/two/covered",
                LineExecutionCounts = new int?[] { null, 2, null, 5, 0 }
            };

            testCaseSummary.CoverageObject["/four/lines/four/covered"] = new CoverageFileData
            {
                FilePath = "/four/lines/four/covered",
                LineExecutionCounts = new int?[] { null, 2, 3, 4, 5 }
            };

            return testCaseSummary;
        }

        private IFileSystemWrapper GetFileSystemWrapper()
        {
            return new Mock<IFileSystemWrapper>().Object;
        }

        [Fact]
        public void Should_Have_Correct_Name()
        {
            Assert.Equal("emma", new EmmaXmlTransformer(GetFileSystemWrapper()).Name);
        }

        [Fact]
        public void Should_Have_Non_Empty_Description()
        {
            Assert.False(string.IsNullOrWhiteSpace(new EmmaXmlTransformer(GetFileSystemWrapper()).Description));
        }

        [Fact]
        public void Should_Throw_If_TestCaseSummary_Is_Null()
        {
            var transformer = new EmmaXmlTransformer(GetFileSystemWrapper());

            Exception ex = Record.Exception(() => transformer.Transform(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void Should_Generate_Xml()
        {
            var transformer = new EmmaXmlTransformer(GetFileSystemWrapper());
            var summary = GetTestCaseSummary();
            var expected =
@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<report>
 <stats>
  <srcfiles value=""3"" />
  <srclines value=""7"" />
 </stats>
 <data>
  <all name=""all classes"">
   <coverage type=""line, %"" value=""86% (6/7)"" />
   <srcfile name=""/three/lines/two/covered"">
    <coverage type=""line, %"" value=""67% (2/3)"" />
   </srcfile>
   <srcfile name=""/four/lines/four/covered"">
    <coverage type=""line, %"" value=""100% (4/4)"" />
   </srcfile>
  </all>
 </data>
</report>
";

            var result = transformer.Transform(summary);

            Assert.Equal(expected.Replace("\r", "").Replace("\n",""), result.Replace("\r", "").Replace("\n", ""));
        }
    }
}
