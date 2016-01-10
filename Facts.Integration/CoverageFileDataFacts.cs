using System.Linq;

using Chutzpah.Models;

using Xunit;

namespace Chutzpah.Facts.Integration
{
    public class CoverageFileDataFacts
    {
        private CoverageFileData data1;

        private CoverageFileData data2;

        public CoverageFileDataFacts()
        {
            this.data1 = new CoverageFileData
            {
                FilePath = "test1",
                LineExecutionCounts = new int?[] { null, 1, 0, 1 },
                SourceLines = new[] { "" }
            };

            this.data2 = new CoverageFileData
            {
                FilePath = "test1",
                LineExecutionCounts = new int?[] { null, 0, 1, 0 },
                SourceLines = new[] { "" }
            };
        }

        [Fact]
        public void Will_CoveragePercentage_calculated_right_for_Data1()
        {
            // Arrange
            // Act
            var percentage = this.data1.CoveragePercentage;

            // Assert
            Assert.Equal(0.67, percentage, 2);
        }

        [Fact]
        public void Will_CoveragePercentage_calculated_right_for_Data2()
        {
            // Arrange
            // Act
            var percentage = this.data2.CoveragePercentage;

            // Assert
            Assert.Equal(0.33, percentage, 2);
        }

        [Fact]
        public void If_call_CoveragePercentage_before_merge_Data1_and_Data2_and_after_merge_CoveragePercentage_changing()
        {
            // Arrange
            var fakePercentage = this.data1.CoveragePercentage;
            this.data1.Merge(this.data2);

            // Act
            var percentage = this.data1.CoveragePercentage;

            // Assert
            Assert.Equal(1, percentage, 2);
        }
    }
}