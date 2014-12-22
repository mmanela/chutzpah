using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Chutzpah.Extensions;
using Chutzpah.Models;
using Chutzpah.VSTS;

namespace Chutzpah.RunnerCallbacks
{
    public class TrxConsoleRunnerCallback : ConsoleRunnerCallback
    {


        private readonly string outputFileName;

        private TestRunType testRun;

        private List<VSTSTestCase> currentTestCases;

        public TrxConsoleRunnerCallback(string outputFilename)
        {
            if (outputFilename == null)
            {
                throw new ArgumentNullException("outputFilename");
            }

            this.outputFileName = outputFilename;
        }

        public override void TestSuiteStarted()
        {
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

            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).creation = DateTime.Now.ToString("O");
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).start = DateTime.Now.ToString("O");
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).queuing = DateTime.Now.ToString("O");

            currentTestCases = new List<VSTSTestCase>();
        }

        public override void TestStarted(TestCase testCase)
        {
            base.TestStarted(testCase);
            var newTestCase = new VSTSTestCase().UpdateWith(testCase);
            newTestCase.StartTime = DateTime.Now;
            currentTestCases.Add(newTestCase);
        }

        protected override void TestFailed(TestCase testCase)
        {
            base.TestFailed(testCase);
            currentTestCases[currentTestCases.IndexOf(new VSTSTestCase().UpdateWith(testCase))].Passed = false;
        }

        protected override void TestPassed(TestCase testCase)
        {
            base.TestPassed(testCase);
            currentTestCases[currentTestCases.IndexOf(new VSTSTestCase().UpdateWith(testCase))].Passed = true;
        }

        public override void TestFinished(TestCase testCase)
        {
            currentTestCases[currentTestCases.IndexOf(new VSTSTestCase().UpdateWith(testCase))].EndTime = DateTime.Now;
            base.TestFinished(testCase);
        }

        public override void ExceptionThrown(Exception exception, string fileName)
        {
            base.ExceptionThrown(exception, fileName);
            var possibleErrorLocation = 0;
            if (!currentTestCases.Any())
                return;

            possibleErrorLocation = currentTestCases.FindLastIndex(x => x.InputTestFile.Equals(fileName));
            if (possibleErrorLocation == -1)
                possibleErrorLocation = currentTestCases.Count() - 1;

            //set the exception.
            currentTestCases[possibleErrorLocation].exception = exception;
        }

        public override void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            var testList = new TestListType
            {
                name = "Results Not in a List",
                id = Guid.NewGuid().ToString()
            };

            var testTypeId = Guid.NewGuid();

            foreach (var testCase in testResultsSummary.Tests)
            {
                currentTestCases[currentTestCases.IndexOf(new VSTSTestCase().UpdateWith(testCase))].UpdateWith(testCase);
            }

            var testsHelper = currentTestCases.ToList();
            testRun.Items.GetInstance<TestRunTypeTimes>(VSTSExtensions.TestRunItemType.Times).finish = DateTime.Now.ToString("O");
            testRun.Items.GetInstance<TestRunTypeResultSummary>(VSTSExtensions.TestRunItemType.ResultSummary).outcome = testsHelper.Count(x=>x.Passed) == testsHelper.Count() ? "Passed" : "Failed";

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
                        Items = new []
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

            testRun.Items.GetInstance<TestEntriesType1>(VSTSExtensions.TestRunItemType.TestEntries).TestEntry = testsHelper.Select(testCase=>new TestEntryType
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
                        duration = testCase.EndTime.Subtract(testCase.StartTime).ToString("c"),
                        startTime = testCase.StartTime.ToString("O"),
                        endTime = testCase.EndTime.ToString("O"),
                        // This is for specific test type.
                        testType = "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b",
                        outcome = testCase.Passed ? "Passed" : "Failed",
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


            using (var f = new FileStream(this.outputFileName, FileMode.Create))
            {
                var xs = new XmlSerializer(typeof (TestRunType));
                xs.Serialize(f, testRun);
            }

            base.TestSuiteFinished(testResultsSummary);
        }

        protected virtual string GetCodeCoverageMessage(CoverageData coverageData)
        {
            return "";
        }
    }
}
