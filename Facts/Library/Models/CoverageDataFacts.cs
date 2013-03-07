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
    }
}