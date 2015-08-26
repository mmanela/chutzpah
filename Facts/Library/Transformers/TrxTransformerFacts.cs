using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Chutzpah.Extensions;
using Chutzpah.Models;
using Chutzpah.Transformers;
using Chutzpah.VSTS;
using Xunit;
using Chutzpah.Wrappers;
using Moq;

namespace Chutzpah.Facts.Library.Transformers
{
    public class TrxTransformerFacts
    {
        private IFileSystemWrapper GetFileSystemWrapper()
        {
            return new Mock<IFileSystemWrapper>().Object;
        }

        private static TestCaseSummary BuildTestCaseSummary()
        {
            var summary = new TestCaseSummary();
            var fileSummary = new TestFileSummary("path1") { TimeTaken = 1500 };
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
            var transformer = new TrxXmlTransformer(GetFileSystemWrapper());

            Exception ex = Record.Exception(() => transformer.Transform(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void Will_generate_trx_xml()
        {
            //Since the generated file is through GUID, we cannot do a simply string compare.

            var transformer = new TrxXmlTransformer(GetFileSystemWrapper());
            var summary = BuildTestCaseSummary();
            var result = transformer.Transform(summary);

            XmlReader xr = new XmlTextReader(new StringReader(result));
            XmlSerializer xs = new XmlSerializer(typeof(TestRunType));

            Assert.True(xs.CanDeserialize(xr));

            TestRunType trx = (TestRunType)xs.Deserialize(xr);


            var testDefinitions =
                trx.Items.GetInstance<TestDefinitionType>(VSTSExtensions.TestRunItemType.TestDefinition).Items.Cast<UnitTestType>().ToArray();

            Assert.Equal(testDefinitions.Count(), 4);

            for (int i = 0; i < testDefinitions.Count(); i++)
            {
                var vststUnitTest = testDefinitions[i];
                var testSummary = summary.Tests[i];

                Assert.Equal(vststUnitTest.TestMethod.name, testSummary.TestName);
                Assert.Equal(vststUnitTest.TestMethod.adapterTypeName, "Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter");
            }

            var testResults =
                trx.Items.GetInstance<ResultsType>(VSTSExtensions.TestRunItemType.Results).Items.Cast<UnitTestResultType>().ToArray();
            Assert.Equal(testResults.Count(), 4);

            for (int i = 0; i < testResults.Count(); i++)
            {
                var vststUnitTestResult = testResults[i];
                var testSummary = summary.Tests[i];

                Assert.Equal(vststUnitTestResult.testName,testSummary.TestName);
                Assert.Equal(vststUnitTestResult.outcome,testSummary.ResultsAllPassed ? "Passed":"Failed");
                if (vststUnitTestResult.Items != null && vststUnitTestResult.Items.Any())
                    Assert.Equal(((OutputType)vststUnitTestResult.Items[0]).ErrorInfo.Message, testSummary.TestResults[0].Message);
            }

            var counters = (CountersType)
                trx.Items.GetInstance<TestRunTypeResultSummary>(VSTSExtensions.TestRunItemType.ResultSummary)
                    .Items.First();

            Assert.Equal(counters.passed,2);
            Assert.Equal(counters.failed,2);
        }
    }
}
