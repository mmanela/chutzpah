using System;
using System.Linq;
using System.Text;
using Chutzpah.Models;
using Encoder = Microsoft.Security.Application.Encoder;
using Chutzpah.Wrappers;

namespace Chutzpah.Transformers
{
    public class JUnitXmlTransformer : SummaryTransformer
    {
        public override string Name
        {
            get { return "junit"; }
        }

        public override string Description
        {
            get { return "output results to JUnit-style XML file"; }
        }

        public JUnitXmlTransformer(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {

        }

        public override string Transform(TestCaseSummary testFileSummary)
        {
            if (testFileSummary == null) throw new ArgumentNullException("testFileSummary");

            var builder = new StringBuilder();
            builder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>");
            builder.AppendLine(@"<testsuites>");
            foreach (TestFileSummary file in testFileSummary.TestFileSummaries)
            {
                builder.AppendLine(
                    string.Format(@"  <testsuite name=""{0}"" tests=""{1}"" failures=""{2}"" time=""{3}"">",
                                  Encode(file.Path), file.Tests.Count, file.Tests.Count(x => !x.Passed), file.TimeTaken));
                ;
                foreach (TestCase test in file.Tests)
                {
                    if (test.Passed)
                    {
                        builder.AppendLine(string.Format(@"    <testcase name=""{0}"" time=""{1}"" />",
                                           Encode(test.GetDisplayName()), test.TimeTaken));
                    }
                    else
                    {
                        TestResult failureCase = test.TestResults.FirstOrDefault(x => !x.Passed);
                        if (failureCase != null)
                        {
                            string failureMessage = failureCase.GetFailureMessage();
                            builder.AppendLine(string.Format(@"    <testcase name=""{0}"" time=""{1}"">",
                                                             Encode(test.GetDisplayName()), test.TimeTaken));
                            builder.AppendLine(string.Format(@"      <failure message=""{0}""></failure>",
                                                             Encode(failureMessage)));
                            builder.AppendLine(string.Format(@"    </testcase>"));
                        }
                    }
                }
                builder.AppendLine(@"  </testsuite>");
            }
            builder.AppendLine(@"</testsuites>");
            return builder.ToString();
        }

        private static string Encode(string str)
        {
            return Encoder.XmlEncode(str);
        }
    }
}