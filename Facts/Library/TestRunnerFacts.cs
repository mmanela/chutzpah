using System;
using System.Collections.Generic;
using System.IO;
using Chutzpah.Facts.Mocks;
using Chutzpah.Frameworks;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestRunnerFacts
    {
        private class TestableTestRunner : Testable<TestRunner>
        {
            public TestableTestRunner()
            {
                IFrameworkDefinition qunitDefinition = new QUnitDefinition();
                Mock<IFrameworkManager>().Setup(x => x.TryDetectFramework(It.IsAny<string>(), out qunitDefinition)).Returns(true);
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns("");
                Mock<IFileProbe>().Setup(x => x.FindScriptFiles(It.IsAny<IEnumerable<string>>())).Returns<IEnumerable<string>>(x => x);
            }
        }

        public class GetTestContext
        {
            [Fact]
            public void Will_get_test_context()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext();
                runner.Mock<ITestContextBuilder>().Setup(x => x.BuildContext("a.js", null)).Returns(context);

                var res = runner.ClassUnderTest.GetTestContext("a.js");

                Assert.Equal(context, res);
            }

            [Fact]
            public void Will_get_test_context_given_staging_folder()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext();
                runner.Mock<ITestContextBuilder>().Setup(x => x.BuildContext("a.js", "staging")).Returns(context);

                var res = runner.ClassUnderTest.GetTestContext("a.js", new TestOptions { StagingFolder = "staging" });

                Assert.Equal(context, res);
            }
        }

        public class RunTests
        {
            [Fact]
            public void Will_throw_if_test_files_collection_is_null()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var ex = Record.Exception(() => runner.ClassUnderTest.RunTests((IEnumerable<string>)null)) as ArgumentNullException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_throw_if_headless_browser_does_not_exist()
            {
                TestableTestRunner runner = new TestableTestRunner();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns((string)null);

                var ex = Record.Exception(() => runner.ClassUnderTest.RunTests("someFile")) as FileNotFoundException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_throw_if_test_runner_js_does_not_exist()
            {
                TestableTestRunner runner = new TestableTestRunner();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns((string)null);

                var ex = Record.Exception(() => runner.ClassUnderTest.RunTests("someFile")) as FileNotFoundException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_build_context_with_given_staging_path()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<ITestContextBuilder>()
                    .Setup(x => x.TryBuildContext(@"path\tests.html", "staging", out context))
                    .Returns(true)
                    .Verifiable();
                string args = TestRunner.TestRunnerJsName + @" file:///D:/harnessPath.html";

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { StagingFolder = "staging" }, testCallback.Object);

                runner.Mock<ITestContextBuilder>().Verify();
            }

            [Fact]
            public void Will_run_test_file_and_return_test_results_model()
            {
                TestableTestRunner runner = new TestableTestRunner();
                BrowserTestFileResult fileResult = null;
                var testResults = new List<TestResult> { new TestResult() };
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                string args = "\"jsPath\"" + @" ""file:///D:/harnessPath.html""";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput("browserPath", args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Callback<BrowserTestFileResult>(x => fileResult = x).Returns(testResults);

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                Assert.Equal(1, res.TotalCount);
                Assert.Equal(context, fileResult.TestContext);
                Assert.Equal("json", fileResult.BrowserOutput);
            }

            [Fact]
            public void Will_run_test_files_found_from_given_folder_path()
            {
                TestableTestRunner runner = new TestableTestRunner();
                BrowserTestFileResult fileResult = null;
                var testResults = new List<TestResult> { new TestResult() };
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                string args = "\"jsPath\"" + @" ""file:///D:/harnessPath.html""";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput("browserPath", args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Callback<BrowserTestFileResult>(x => fileResult = x).Returns(testResults);
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\testFolder" }))
                    .Returns(new List<string> { @"path\tests.html" });

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\testFolder");

                Assert.Equal(1, res.TotalCount);
                Assert.Equal(context, fileResult.TestContext);
                Assert.Equal("json", fileResult.BrowserOutput);
            }

            [Fact]
            public void Will_open_test_file_in_browser_when_given_flag()
            {
                TestableTestRunner runner = new TestableTestRunner();
                BrowserTestFileResult fileResult = null;
                var testResults = new List<TestResult> { new TestResult() };
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                string args = "\"jsPath\"" + @" ""file:///D:/harnessPath.html""";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput("browserPath", args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Callback<BrowserTestFileResult>(x => fileResult = x).Returns(testResults);

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { OpenInBrowser = true });

                runner.Mock<IProcessHelper>().Verify(x => x.LaunchFileInBrowser(@"D:\harnessPath.html"));
            }

            [Fact]
            public void Will_run_multiple_test_files_and_return_results()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var fileResults = new List<BrowserTestFileResult>();
                var testResults = new List<TestResult> { new TestResult() };
                var context1 = new TestContext { TestHarnessPath = @"D:\harnessPath1.html" };
                var context2 = new TestContext { TestHarnessPath = @"D:\harnessPath2.htm" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests1.html", null, out context1)).Returns(true);
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests2.htm", null, out context2)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests1.html")).Returns(@"D:\path\tests1.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests2.htm")).Returns(@"D:\path\tests2.htm");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                string args1 = "\"jsPath\"" + @" ""file:///D:/harnessPath1.html""";
                string args2 = "\"jsPath\"" + @" ""file:///D:/harnessPath2.htm""";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput(It.IsAny<string>(), args1)).Returns("json1");
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput(It.IsAny<string>(), args2)).Returns("json2");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Callback<BrowserTestFileResult>(fileResults.Add).Returns(testResults);
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\tests1a.html", @"path\tests2a.htm" }))
                    .Returns(new List<string> { @"path\tests1.html", @"path\tests2.htm" });

                TestResultsSummary res = runner.ClassUnderTest.RunTests(new List<string> { @"path\tests1a.html", @"path\tests2a.htm" });

                Assert.Equal(2, res.TotalCount);
                Assert.Equal(context1, fileResults[0].TestContext);
                Assert.Equal("json1", fileResults[0].BrowserOutput);
                Assert.Equal(context2, fileResults[1].TestContext);
                Assert.Equal("json2", fileResults[1].BrowserOutput);
            }

            [Fact]
            public void Will_call_test_suite_started()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                string args = TestRunner.TestRunnerJsName + @" file:///D:/harnessPath.html";

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteStarted());
            }

            [Fact]
            public void Will_call_test_suite_finished_with_final_result()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var testResults = new List<TestResult> { new TestResult() };
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                string args = "jsPath" + @" file:///D:/harnessPath.html";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput(It.IsAny<string>(), args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Returns(testResults);

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteFinished(It.IsAny<TestResultsSummary>()));
            }

            [Fact]
            public void Will_call_file_started_on_callback_and_stop_test_if_return_is_false()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", InputTestFile = @"D:\path\tests.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                testCallback.Setup(x => x.FileStart(@"D:\path\tests.html")).Returns(false).Verifiable();
                string args = TestRunner.TestRunnerJsName + @" file:///D:/harnessPath.html";

                TestResultsSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify();
                runner.Mock<IProcessHelper>().Verify(x => x.RunExecutableAndCaptureOutput(TestRunner.HeadlessBrowserName, args), Times.Never());
            }

            [Fact]
            public void Will_call_file_finished_on_callback_and_stop_test_if_return_is_false()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testResults = new List<TestResult> { new TestResult() };
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", InputTestFile = @"D:\path\tests.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                testCallback.Setup(x => x.FileFinished(@"D:\path\tests.html", It.Is<TestResultsSummary>(t => t.TotalCount == 1))).Returns(false).Verifiable();
                string args = TestRunner.TestRunnerJsName + @" file:///D:/harnessPath.html";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput(TestRunner.HeadlessBrowserName, args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Returns(testResults);

                TestResultsSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html", @"path\tests.html" }, testCallback.Object);

                testCallback.Verify();
                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_call_test_finished_on_callback_for_each_test()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testResults = new List<TestResult> { new TestResult(), new TestResult() };
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests.html", null, out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                string args = TestRunner.TestRunnerJsName + @" file:///D:/harnessPath.html";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput(TestRunner.HeadlessBrowserName, args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Returns(testResults);

                TestResultsSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html", @"path\tests.html" }, testCallback.Object);

                testCallback.Verify(x => x.TestFinished(testResults[0]));
                testCallback.Verify(x => x.TestFinished(testResults[1]));
            }

            [Fact]
            public void Will_call_exception_thrown_on_callback_and_move_to_next_test_file()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var testResults = new List<TestResult> { new TestResult() };
                var testCallback = new MockTestMethodRunnerCallback();
                var exception = new Exception();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests1.html", null, out context)).Throws(exception);
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(@"path\tests2.html", null, out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests1.html")).Throws(exception);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests2.html")).Returns(@"D:\path\tests2.html");
                string args = TestRunner.TestRunnerJsName + @" file:///D:/harnessPath.html";
                runner.Mock<IProcessHelper>().Setup(x => x.RunExecutableAndCaptureOutput(TestRunner.HeadlessBrowserName, args)).Returns("json");
                runner.Mock<ITestResultsBuilder>().Setup(x => x.Build(It.IsAny<BrowserTestFileResult>())).Returns(testResults);

                TestResultsSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests1.html", @"path\tests2.html" }, testCallback.Object);

                testCallback.Verify(x => x.ExceptionThrown(exception, @"path\tests1.html"));
                Assert.Equal(1, res.TotalCount);
            }
        }
    }
}