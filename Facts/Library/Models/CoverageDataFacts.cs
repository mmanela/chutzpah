using System;
using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Library.Models
{
    public class CoverageDataFacts
    {
        [Fact] 
        public void Will_calculate_coverage_percentage()
        {
            var coverageData = new CoverageData();
            coverageData["file1"] = new CoverageFileData();
            coverageData["file2"] = new CoverageFileData();
            coverageData["file1"].LineExecutionCounts = new int?[]{/* index 0 ignored */ 0,0,1, null,0,1};
            coverageData["file2"].LineExecutionCounts = new int?[]{/* index 0 ignored */ 0,5,1,2,1, null};

            Assert.Equal(0.5, coverageData["file1"].CoveragePercentage);
            Assert.Equal(1, coverageData["file2"].CoveragePercentage);
            Assert.Equal(0.75, coverageData.CoveragePercentage);
        }

        [Fact]
        public void Will_merge_coverage_object_with_an_empty_one()
        {
            var coverageData = new CoverageData();
            coverageData["file1"] = new CoverageFileData();
            coverageData["file2"] = new CoverageFileData();
            coverageData["file1"].LineExecutionCounts = new int?[] {/* index 0 ignored */ 0, 0, 1, null, 0, 1 };
            coverageData["file2"].LineExecutionCounts = new int?[] {/* index 0 ignored */ 0, 5, 1, 2, 1, null };

            var newCoverageData = new CoverageData();
            newCoverageData.Merge(coverageData);

            Assert.Equal(0.5, newCoverageData["file1"].CoveragePercentage);
            Assert.Equal(1, newCoverageData["file2"].CoveragePercentage);
            Assert.Equal(0.75, newCoverageData.CoveragePercentage);
        }

        [Fact]
        public void Will_merge_coverage_object_with_an_existing_one()
        {
            var coverageData1 = new CoverageData();
            coverageData1["file1"] = new CoverageFileData();
            coverageData1["file1"].LineExecutionCounts = new int?[] {/* index 0 ignored */ 0, 0, 2, 1, 0, 0 };

            var coverageData2 = new CoverageData();
            coverageData2["file1"] = new CoverageFileData();
            coverageData2["file2"] = new CoverageFileData();
            coverageData2["file1"].LineExecutionCounts = new int?[] {/* index 0 ignored */ 0, 0, 1, null, 0, 1 };
            coverageData2["file2"].LineExecutionCounts = new int?[] {/* index 0 ignored */ 0, 5, 1, 2, 1, null };
            
            coverageData1.Merge(coverageData2);
            Assert.Equal(0.6, coverageData1["file1"].CoveragePercentage);
            Assert.Equal(1, coverageData1["file2"].CoveragePercentage);
            Assert.Equal(0.778, Math.Round(coverageData1.CoveragePercentage,3));
        }
    }
}