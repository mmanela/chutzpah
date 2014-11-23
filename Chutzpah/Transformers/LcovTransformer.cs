using Chutzpah.Models;
using Chutzpah.Wrappers;
using System;
using System.Text;

namespace Chutzpah.Transformers
{
    public class LcovTransformer : SummaryTransformer
    {
        const string SOURCE_FILE_LINE_FORMAT = "SF:{0}";
        const string LINE_FORMAT = "DA:{0},{1}";
        const string FILE_DELIMITER = "end_of_record";

        public override string Name
        {
            get { return "lcov"; }
        }

        public override string Description
        {
            get { return "outputs results as LCOV data for further processing"; }
        }

        public LcovTransformer(IFileSystemWrapper fileSystem)
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

            var sb = new StringBuilder();
            foreach (var coverageData in testFileSummary.CoverageObject.Values)
            {
                AppendCoverageForFile(sb, coverageData);
            }

            return sb.ToString();
        }

        private void AppendCoverageForFile(StringBuilder builder, CoverageFileData data)
        {
            var counts = data.LineExecutionCounts ?? new int?[0];

            builder.AppendLine(string.Format(SOURCE_FILE_LINE_FORMAT, data.FilePath));
            if (counts.Length > 1)
            {
                for (var i = 1; i < counts.Length; i++)
                {
                    if (counts[i].HasValue)
                    {
                        builder.AppendLine(string.Format(LINE_FORMAT, i, counts[i]));
                    }
                }
            }

            builder.AppendLine(FILE_DELIMITER);
        }
    }
}
