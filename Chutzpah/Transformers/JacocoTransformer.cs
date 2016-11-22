using Chutzpah.Models;
using Chutzpah.Wrappers;
using System;
using System.Text;

namespace Chutzpah.Transformers
{
    /// <summary>
    /// Outputs an XML file with code coverage results in Jacoco format.
    /// </summary>
    public class JacocoTransformer : SummaryTransformer
    {
        private int TotalSourceFiles { get; set; }

        private int TotalSourceLines { get; set; }

        private int TotalSourceLinesCovered { get; set; }

        public override string Name
        {
            get { return "jacoco"; }
        }

        public override string Description
        {
            get { return "output coverage results to an Jacoco-style XML file"; }
        }

        public JacocoTransformer(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {

        }

        public override string Transform(TestCaseSummary testFileSummary)
        {
            if (testFileSummary == null)
            {
                throw new ArgumentNullException("testFileSummary");
            }
            else if (testFileSummary.CoverageObject == null)
            {
                return string.Empty;
            }


            GetOverallStats(testFileSummary.CoverageObject);

            var builder = new StringBuilder();
            builder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>");
            builder.AppendLine(@"<report name=""Chutzpah Coverage"">");
            builder.AppendLine(@"  <package name=""Chutzpah Coverage"">");

            AppendCoverageBySourceFile(builder, testFileSummary.CoverageObject);
            builder.AppendLine(@"  </package>");

            AppendOverallCoverage(builder);

            builder.AppendLine(@"</report>");
            return builder.ToString();
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
            builder.AppendLine($@"   <sourcefile name=""{fileName}"">");

            for (var i = 1; i < fileData.LineExecutionCounts.Length; i++)
            {
                var lineExecution = fileData.LineExecutionCounts[i];

                if (lineExecution.HasValue)
                {
                    var missedCount = lineExecution.Value <= 0 ? 1 : 0;
                    var coveredCount = lineExecution.Value;

                    builder.AppendLine($@"     <line nr=""{i}"" mi=""{missedCount}"" ci=""{coveredCount}""/> ");

                    totalStatements++;
                    if (coveredCount > 0)
                    {
                        statementsCovered += 1; 
                    }
                }
            }

            AppendLineCoverageForSourceFile(builder, statementsCovered, totalStatements);
            builder.AppendLine(@"   </sourcefile>");
        }

        private void GetOverallStats(CoverageData coverage)
        {
            foreach (var pair in coverage)
            {
                TotalSourceFiles += 1;
                var fileData = pair.Value;
                var totalStatements = 0;
                if (fileData.LineExecutionCounts == null)
                {
                    continue;
                }

                for (var i = 1; i < fileData.LineExecutionCounts.Length; i++)
                {
                    var lineExecution = fileData.LineExecutionCounts[i];
                    if (lineExecution.HasValue)
                    {
                        totalStatements++;

                        if (lineExecution > 0)
                        {
                            TotalSourceLinesCovered += 1;
                        }
                    }
                }

                TotalSourceLines += totalStatements;
            }
        }

        private void AppendLineCoverageForSourceFile(StringBuilder builder, int statementsCovered, int totalStatements)
        {
            builder.AppendLine($@"     <counter type=""LINE"" missed=""{totalStatements - statementsCovered}"" covered=""{statementsCovered}"" />");
        }

        private void AppendOverallCoverage(StringBuilder builder)
        {
            var overall = $@"  <counter type=""LINE"" missed=""{TotalSourceLines - TotalSourceLinesCovered}"" covered=""{TotalSourceLinesCovered}"" />";
 
            builder.AppendLine(overall);
        }
    }
}
