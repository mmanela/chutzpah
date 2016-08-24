using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Chutzpah.Extensions;
using Chutzpah.Models;
using Chutzpah.VSTS;
using Chutzpah.Wrappers;

namespace Chutzpah.Transformers
{
    public class TrxXmlTransformer : SummaryTransformer
    {
        private TestRunType testRun;

        public TrxXmlTransformer(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public override string Name
        {
            get { return "trx"; }
        }

        public override string Description
        {
            get { return "output results to Visual Studio Trx file"; }
        }

        public override string Transform(TestCaseSummary testFileSummary)
        {
            if (testFileSummary == null) throw new ArgumentNullException("testFileSummary");


            testRun = new TestRunType
            {
                id = Guid.NewGuid().ToString(),
                name = "Chutzpah_JS_UnitTest_" + DateTime.Now.ToString("yy-MMM-dd hh:mm:ss zz")
            };

            var windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            if (windowsIdentity != null)
                testRun.runUser = windowsIdentity.Name;

            testRun.Items = new object[]
            {
                new TestRunTypeResultSummary(),
                new ResultsType(),
                new TestDefinitionType(),
                new TestEntriesType1(),
                new TestRunTypeTestLists(),
                new TestRunTypeTimes(),
                new TestSettingsType
                {
                    name = "Default",
                    id = Guid.NewGuid().ToString(),
                    Execution = new TestSettingsTypeExecution
                    {
                        TestTypeSpecific = new TestSettingsTypeExecutionTestTypeSpecific{}
                    }
                }
            };
            // Time taken is current time 
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).creation = DateTime.Now.AddSeconds(-testFileSummary.TimeTaken).ToString("O");
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).start = DateTime.Now.AddSeconds(-testFileSummary.TimeTaken).ToString("O");
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).queuing = DateTime.Now.AddSeconds(-testFileSummary.TimeTaken).ToString("O");


            var testList = new TestListType
            {
                name = "Results Not in a List",
                id = Guid.NewGuid().ToString()
            };

            var testTypeId = Guid.NewGuid();

            var currentTestCases = testFileSummary.Tests.Select(x => new VSTSTestCase().UpdateWith(x));


            var testsHelper = currentTestCases.ToList();
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).finish = DateTime.Now.ToString("O");
            testRun.Items.GetInstance<TestRunTypeResultSummary>(VSTSExtensions.TestRunItemType.ResultSummary).outcome = testsHelper.Count(x => x.Passed) == testsHelper.Count() ? "Passed" : "Failed";

            var counter = new CountersType
            {
                aborted = 0,
                completed = 0,
                disconnected = 0,
                error = 0,
                passed = testsHelper.Count(x => x.Passed),
                executed = testsHelper.Count,
                failed = testsHelper.Count(x => !x.Passed),
                total = testsHelper.Count,
                inProgress = 0,
                pending = 0,
                warning = 0,
                notExecuted = 0,
                notRunnable = 0,
                passedButRunAborted = 0,
                inconclusive = 0,
                timeout = 0
            };

            // total attribute is not written if totalSpecified is false
            counter.totalSpecified = true;

            testRun.Items.GetInstance<TestRunTypeResultSummary>(VSTSExtensions.TestRunItemType.ResultSummary).Items = new object[]
            {
                counter
            };

            testRun.Items.GetInstance<TestDefinitionType>(VSTSExtensions.TestRunItemType.TestDefinition).Items = testsHelper
                .Select(
                    (testCase) => new UnitTestType
                    {
                        id = testCase.Id.ToString(),
                        name = testCase.TestName,
                        storage = testCase.InputTestFile,
                        Items = new[]
                        {
                            new BaseTestTypeExecution
                            {
                                id= testCase.ExecutionId.ToString()
                            }
                        },
                        TestMethod = new UnitTestTypeTestMethod
                        {
                            adapterTypeName = "Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAdapter",
                            className = Path.GetFileNameWithoutExtension(testCase.InputTestFile),
                            codeBase = testCase.InputTestFile,
                            name = testCase.TestName
                        }
                    }).ToArray();

            testRun.Items.GetInstance<TestDefinitionType>(VSTSExtensions.TestRunItemType.TestDefinition).ItemsElementName = testsHelper
                    .Select(
                        (testCase) => ItemsChoiceType4.UnitTest).ToArray();

            testRun.Items.GetInstance<TestRunTypeTestLists>(VSTSExtensions.TestRunItemType.TestLists).TestList = new[]
            {
                testList,
                // This has to be hard-coded.
                new TestListType
                {
                    name = "All Loaded Results",
                    id = "19431567-8539-422a-85d7-44ee4e166bda"
                }
            };

            testRun.Items.GetInstance<TestEntriesType1>(VSTSExtensions.TestRunItemType.TestEntries).TestEntry = testsHelper.Select(testCase => new TestEntryType
            {
                testId = testCase.Id.ToString(),
                executionId = testCase.ExecutionId.ToString(),
                testListId = testList.id
            }).ToArray();

            testRun.Items.GetInstance<ResultsType>(VSTSExtensions.TestRunItemType.Results).Items = testsHelper.Select((testCase) =>
            {
                var unitTestResultType = new UnitTestResultType
                {
                    executionId = testCase.ExecutionId.ToString(),
                    testId = testCase.Id.ToString(),
                    testName = testCase.TestName,
                    computerName = Environment.MachineName,
                    duration = new TimeSpan(0, 0, testCase.TimeTaken).ToString("c"),
                    // I tried adding this to StandardConsoleRunner, but it demanded too many changes.
                    // Setting start to the creation date.
                    startTime = DateTime.Now.AddSeconds(-testFileSummary.TimeTaken).ToString("O"),
                    // Setting end time to creation date + time taken to run this test.
                    endTime = DateTime.Now.AddSeconds((-testFileSummary.TimeTaken) + testCase.TimeTaken).ToString("O"),
                    // This is for specific test type.
                    testType = "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b",
                    outcome = testCase.TestOutcome == Models.TestOutcome.Passed ? "Passed" : 
                                                      testCase.TestOutcome == Models.TestOutcome.Skipped ? "NotExecuted" : "Failed",
                    testListId = testList.id,

                };

                if (!testCase.Passed)
                {
                    unitTestResultType.Items = new[]
                        {
                            new OutputType()
                            {
                                ErrorInfo = new OutputTypeErrorInfo{Message = testCase.exception !=null ? testCase.exception.ToString(): string.Join(",",testCase.TestResults.Where(x=>!x.Passed).Select(x=>x.Message))}
                            }
                        };
                }
                return unitTestResultType;

            }).ToArray();


            testRun.Items.GetInstance<ResultsType>(VSTSExtensions.TestRunItemType.Results).ItemsElementName =
                testsHelper.Select(testCase => ItemsChoiceType3.UnitTestResult).ToArray();

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

            var stringStream = new StringWriter();
            var xs = new XmlSerializer(typeof(TestRunType));
            xs.Serialize(stringStream, testRun, ns);

            return stringStream.ToString();
        }
    }
}
