using System;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Transformers;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class JUnitXmlTransformerFacts
    {
        private static TestCaseSummary BuildTestCaseSummary()
        {
            var summary = new TestCaseSummary();
            var fileSummary = new TestFileSummary("path1"){ TimeTaken = 1500};
            fileSummary.AddTestCase(new TestCase
            {
                ModuleName = "module1",
                TestName = "test1",
                TestResults = new List<TestResult> { new TestResult { Passed = false, Message = "some failure" } },
                TimeTaken = 1000
            });
            fileSummary.AddTestCase(new TestCase
            {
                ModuleName = "module1",
                TestName = "test2",
                TestResults = new List<TestResult> { new TestResult { Passed = true } },
                TimeTaken = 500
            });

            var fileSummary2 = new TestFileSummary("path>2") { TimeTaken = 2000 };
            fileSummary2.AddTestCase(new TestCase
            {
                TestName = "test3",
                TestResults = new List<TestResult> { new TestResult { Passed = true } },
                TimeTaken = 1000
            });
            fileSummary2.AddTestCase(new TestCase
            {
                TestName = "test<4",
                TestResults = new List<TestResult> { new TestResult { Passed = false, Message = "bad<failure" } },
                TimeTaken = 1000
            });

            summary.Append(fileSummary);
            summary.Append(fileSummary2);
            return summary;
        }

        [Fact]
        public void Will_throw_if_test_summary_is_null()
        {
            var transformer = new JUnitXmlTransformer();

            Exception ex = Record.Exception(() => transformer.Transform(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void Will_generate_junit_xml()
        {
            var transformer = new JUnitXmlTransformer();
            var summary = BuildTestCaseSummary();
            var expected =
                @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<testsuites>
  <testsuite name=""path1"" tests=""2"" failures=""1"" time=""1500"">
    <testcase name=""module1:test1"" time=""1000"">
      <failure message=""some failure""></failure>
    </testcase>
    <testcase name=""module1:test2"" time=""500"" />
  </testsuite>
  <testsuite name=""path&gt;2"" tests=""2"" failures=""1"" time=""2000"">
    <testcase name=""test3"" time=""1000"" />
    <testcase name=""test&lt;4"" time=""1000"">
      <failure message=""bad&lt;failure""></failure>
    </testcase>
  </testsuite>
</testsuites>
";

            var result = transformer.Transform(summary);

            Assert.Equal(expected, result);
        }
    }
}