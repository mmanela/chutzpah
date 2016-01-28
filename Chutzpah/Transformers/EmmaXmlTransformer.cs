using Chutzpah.Models;
using Chutzpah.Wrappers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace Chutzpah.Transformers
{
    /// <summary>
    /// Outputs an XML file with a schema similar to the one found at http://emma.sourceforge.net/coverage_sample_c/coverage.xml
    /// </summary>
    public class EmmaXmlTransformer : SummaryTransformer
    {
        private int TotalSourceFiles { get; set; }

        private int TotalSourceLines { get; set; }

        private int TotalSourceLinesCovered { get; set; }

        public override string Name
        {
            get { return "emma"; }
        }

        public override string Description
        {
            get { return "output results to Emma-style XML file"; }
        }

        public EmmaXmlTransformer(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {

        }

        public override string Transform(TestCaseSummary testFileSummary)
        {
            if (testFileSummary == null)
            {
                throw new ArgumentNullException("testFileSummary");
            }

            GetOverallStats(testFileSummary.CoverageObject);
            var builder = new StringBuilder();
            builder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>");
            builder.AppendLine(@"<report>");
            AppendOverallStats(builder, testFileSummary.CoverageObject);
            builder.AppendLine(@" <data>");
            builder.AppendLine(@"  <all name=""all classes"">");
            AppendOverallCoverage(builder);
            AppendCoverageBySourceFile(builder, testFileSummary.CoverageObject);
            builder.AppendLine(@"  </all>");
            builder.AppendLine(@" </data>");
            builder.AppendLine(@"</report>");
            return builder.ToString();
        }

        private void AppendOverallStats(StringBuilder builder, CoverageData coverage)
        {
            builder.AppendLine(@" <stats>");
            builder.AppendLine(String.Format(@"  <srcfiles value=""{0}"" />", this.TotalSourceFiles));
            builder.AppendLine(String.Format(@"  <srclines value=""{0}"" />", this.TotalSourceLines));
            builder.AppendLine(@" </stats>");
        }

        private void AppendCoverageBySourceFile(StringBuilder builder, CoverageData coverage)
        {
            foreach (var pair in coverage)
            {
                var fileName = pair.Key;
                var fileData = pair.Value;

                if (fileData.LineExecutionCounts == null)
                {
                    continue;
                }

                AppendCoverageForOneSourceFile(builder, fileName, fileData);
            }
        }

        private void AppendCoverageForOneSourceFile(StringBuilder builder, string fileName, CoverageFileData fileData)
        {
            var totalStatements = 0;
            var statementsCovered = 0;
            builder.AppendLine(String.Format(@"   <srcfile name=""{0}"">", fileName));

            for (var i = 1; i < fileData.LineExecutionCounts.Length; i++)
            {
                var lineExecution = fileData.LineExecutionCounts[i];
                if (lineExecution.HasValue)
                {
                    totalStatements++;
                    if (lineExecution > 0)
                    {
                        statementsCovered += 1;
                    }
                }
            }

            AppendLineCoverageForSourceFile(builder, statementsCovered, totalStatements);
            builder.AppendLine(@"   </srcfile>");
        }

        private void GetOverallStats(CoverageData coverage)
        {
            foreach (var pair in coverage)
            {
                this.TotalSourceFiles += 1;
                var fileData = pair.Value;
                var totalSmts = 0;
                if (fileData.LineExecutionCounts == null)
                {
                    continue;
                }

                for (var i = 1; i < fileData.LineExecutionCounts.Length; i++)
                {
                    var lineExecution = fileData.LineExecutionCounts[i];
                    if (lineExecution.HasValue)
                    {
                        totalSmts++;

                        if (lineExecution > 0)
                        {
                            this.TotalSourceLinesCovered += 1;
                        }
                    }
                }
                this.TotalSourceLines += totalSmts;
            }
        }

        private void AppendLineCoverageForSourceFile(StringBuilder builder, int statementsCovered, int totalStatements)
        {
            var lineCoverage = String.Format(
                @"    <coverage type=""line, %"" value=""{0}% ({1}/{2})"" />",
                FormatPercentage(statementsCovered, totalStatements),
                statementsCovered,
                totalStatements);
            builder.AppendLine(lineCoverage);
        }

        private void AppendOverallCoverage(StringBuilder builder)
        {
            var overall = String.Format(
                @"   <coverage type=""line, %"" value=""{0}% ({1}/{2})"" />",
                FormatPercentage(this.TotalSourceLinesCovered, this.TotalSourceLines),
                this.TotalSourceLinesCovered,
                this.TotalSourceLines);
            builder.AppendLine(overall);
        }

        private static double FormatPercentage(int number, int total)
        {
            // with no fractional digits
            return Math.Round((number / (double)total) * 100, 0);
        }
    }
}
