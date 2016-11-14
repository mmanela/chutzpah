using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using System;
using Chutzpah.Transformers;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class JacocoTransformerFacts
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
            Assert.Equal("jacoco", new JacocoTransformer(GetFileSystemWrapper()).Name);
        }

        [Fact]
        public void Should_Have_Non_Empty_Description()
        {
            Assert.False(string.IsNullOrWhiteSpace(new JacocoTransformer(GetFileSystemWrapper()).Description));
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
            var transformer = new JacocoTransformer(GetFileSystemWrapper());
            var summary = GetTestCaseSummary();
            var expected =
@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<report name=""Chutzpah Coverage"">
  <package name=""Chutzpah Coverage"">
   <sourcefile name=""/three/lines/two/covered"">
     <line nr=""1"" mi=""0"" ci=""2""/> 
     <line nr=""3"" mi=""0"" ci=""5""/> 
     <line nr=""4"" mi=""1"" ci=""0""/> 
     <counter type=""LINE"" missed=""1"" covered=""2"" />
   </sourcefile>
   <sourcefile name=""/four/lines/four/covered"">
     <line nr=""1"" mi=""0"" ci=""2""/> 
     <line nr=""2"" mi=""0"" ci=""3""/> 
     <line nr=""3"" mi=""0"" ci=""4""/> 
     <line nr=""4"" mi=""0"" ci=""5""/> 
     <counter type=""LINE"" missed=""0"" covered=""4"" />
   </sourcefile>
  </package>
  <counter type=""LINE"" missed=""1"" covered=""6"" />
</report>
";

            var result = transformer.Transform(summary);

            Assert.Equal(expected.Replace("\r", "").Replace("\n",""), result.Replace("\r", "").Replace("\n", ""));
        }
    }
}
