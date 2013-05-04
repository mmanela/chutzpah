using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah.Exceptions;
using Chutzpah.Facts.Mocks;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestRunnerFacts
    {
        private class TestableTestRunner : Testable<TestRunner>
        {
            public static string ExecutionPhantomArgs = BuildArgs("runner.js","file:///D:/harnessPath.html", "execution",Constants.DefaultTestFileTimeout);

            public TestableTestRunner()
            {
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns("");
                Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(It.IsAny<IEnumerable<string>>(), It.IsAny<TestingMode>()))
                    .Returns<IEnumerable<string>,TestingMode>((x,y) => x.Select(f => new PathInfo{ Path = f, FullPath = f}));
                Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>(0, new TestFileSummary("somePath")));
            }

            public static string BuildArgs(string runner, string harness, string mode="execution", int? timeout = null)
            {
                var format = "{0} \"{1}\" \"{2}\" {3} {4}";
                return string.Format(format, "--proxy-type=none", runner, harness, mode, timeout.HasValue ? timeout.ToString() : "");
            }
        }

        public class CleanContext
        {
            [Fact]
            public void Will_clean_test_context()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext();

                runner.ClassUnderTest.CleanTestContext(context);

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context));
            }
        }

        public class GetTestContext
        {
            [Fact]
            public void Will_get_test_context()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext();
                runner.Mock<ITestContextBuilder>().Setup(x => x.BuildContext("a.js", It.IsAny<TestOptions>())).Returns(context);

                var res = runner.ClassUnderTest.GetTestContext("a.js");

                Assert.Equal(context, res);
            }

            [Fact]
            public void Will_return_null_given_empty_path()
            {
                var runner = new TestableTestRunner();

                var result = runner.ClassUnderTest.GetTestContext(string.Empty);

                Assert.Null(result);
            }
        }


        public class IsTestFile
        {
            [Fact]
            public void Will_true_if_test_file()
            {
                var runner = new TestableTestRunner();
                runner.Mock<ITestContextBuilder>().Setup(x => x.IsTestFile("a.js")).Returns(true);

                var result = runner.ClassUnderTest.IsTestFile("a.js");

                Assert.True(result);
            }

            [Fact]
            public void Will_false_if_not_test_file()
            {
                var runner = new TestableTestRunner();
                runner.Mock<ITestContextBuilder>().Setup(x => x.IsTestFile("a.js")).Returns(false);

                var result = runner.ClassUnderTest.IsTestFile("a.js");

                Assert.False(result);
            }
        }

        public class DiscoverTests
        {
            [Fact]
            public void Will_throw_if_test_files_collection_is_null()
            {
                TestableTestRunner runner = new TestableTestRunner();
                var ex = Record.Exception(() => runner.ClassUnderTest.DiscoverTests((IEnumerable<string>)null)) as ArgumentNullException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_throw_if_headless_browser_does_not_exist()
            {
                TestableTestRunner runner = new TestableTestRunner();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns((string)null);

                var ex = Record.Exception(() => runner.ClassUnderTest.DiscoverTests("someFile")) as FileNotFoundException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_throw_if_test_runner_js_does_not_exist()
            {
                TestableTestRunner runner = new TestableTestRunner();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns((string)null);

                var ex = Record.Exception(() => runner.ClassUnderTest.DiscoverTests("someFile")) as FileNotFoundException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_run_test_file_and_return_test_summary_model()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "runner" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<PathInfo>(f => f.FullPath == @"path\tests.html"), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "file:///D:/harnessPath.html", "discovery", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>(0, summary));

                var res = runner.ClassUnderTest.DiscoverTests(@"path\tests.html");

                Assert.Equal(1, res.Count());
            }
        }

        public class RunTests
        {
            [Fact]
            public void Will_throw_if_test_files_collection_is_null()
            {
                var runner = new TestableTestRunner();
                var ex = Record.Exception(() => runner.ClassUnderTest.RunTests((IEnumerable<string>)null)) as ArgumentNullException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_throw_if_headless_browser_does_not_exist()
            {
                var runner = new TestableTestRunner();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns((string)null);

                var ex = Record.Exception(() => runner.ClassUnderTest.RunTests("someFile")) as FileNotFoundException;

                Assert.NotNull(ex);
            }

            [Fact]
            public void Will_throw_if_test_runner_js_does_not_exist()
            {
                var runner = new TestableTestRunner();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns((string)null);

                var ex = Record.Exception(() => runner.ClassUnderTest.RunTests("someFile")) as FileNotFoundException;

                Assert.NotNull(ex);
            }
            
            [Fact]
            public void Will_run_test_file_and_return_test_results_model()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "runner" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<PathInfo>(f => f.FullPath == @"path\tests.html"), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "file:///D:/harnessPath.html", "execution", Constants.DefaultTestFileTimeout);

                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>(0, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_pass_timeout_option_to_test_runner()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "runner" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "file:///D:/harnessPath.html", "execution", 5000);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                 .Returns(new ProcessResult<TestFileSummary>(0, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html",new TestOptions{TestFileTimeoutMilliseconds = 5000});

                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_use_timeout_from_context_if_exists()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "runner", TestFileSettings = new ChutzpahTestSettingsFile{ TestFileTimeout = 6000} };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "file:///D:/harnessPath.html", "execution", 6000);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                 .Returns(new ProcessResult<TestFileSummary>(0, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestFileTimeoutMilliseconds = 5000 });

                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_pass_testing_mode_option_to_test_runner()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "runner" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\testFolder" }, TestingMode.HTML))
                    .Returns(new List<PathInfo> { new PathInfo{ FullPath = @"path\tests.html"} });
                var args = TestableTestRunner.BuildArgs("jsPath", "file:///D:/harnessPath.html", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                 .Returns(new ProcessResult<TestFileSummary>(0, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\testFolder", new TestOptions{ TestingMode = TestingMode.HTML});

                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_run_test_files_found_from_given_folder_path()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "runner" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\testFolder" }, It.IsAny<TestingMode>()))
                    .Returns(new List<PathInfo> { new PathInfo { FullPath = @"path\tests.html" } });
                var args = TestableTestRunner.BuildArgs("jsPath", "file:///D:/harnessPath.html", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                 .Returns(new ProcessResult<TestFileSummary>(0, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\testFolder");

                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_open_test_file_in_browser_when_given_flag()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "testRunner.js"};
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { OpenInBrowser = true });

                runner.Mock<IProcessHelper>().Verify(x => x.LaunchFileInBrowser(@"D:\harnessPath.html"));
            }

            [Fact]
            public void Will_run_multiple_test_files_and_return_results()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context1 = new TestContext { TestHarnessPath = @"D:\harnessPath1.html", TestRunner = "runner1" };
                var context2 = new TestContext { TestHarnessPath = @"D:\harnessPath2.htm", TestRunner = "runner2" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<PathInfo>(f => f.FullPath == @"path\tests1.html"), It.IsAny<TestOptions>(), out context1)).Returns(true);
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<PathInfo>(f => f.FullPath == @"path\tests2.html"), It.IsAny<TestOptions>(), out context2)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests1.html")).Returns(@"D:\path\tests1.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests2.htm")).Returns(@"D:\path\tests2.htm");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner1")).Returns("jsPath1");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner2")).Returns("jsPath2");
                var args1 = TestableTestRunner.BuildArgs("jsPath1", "file:///D:/harnessPath1.html", "execution", Constants.DefaultTestFileTimeout);
                var args2 = TestableTestRunner.BuildArgs("jsPath2", "file:///D:/harnessPath2.htm", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), args1, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>(0, summary));
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), args2, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>(0, summary));
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\tests1a.html", @"path\tests2a.htm" }, It.IsAny<TestingMode>()))
                    .Returns(new List<PathInfo> { new PathInfo { FullPath = @"path\tests1.html" }, new PathInfo { FullPath = @"path\tests2.html" } });
                
                TestCaseSummary res = runner.ClassUnderTest.RunTests(new List<string> { @"path\tests1a.html", @"path\tests2a.htm" });

                Assert.Equal(2, res.TotalCount);
            }

            [Fact]
            public void Will_call_test_suite_started()
            {
                var runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
           
                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteStarted());
            }

            [Fact]
            public void Will_call_test_suite_finished_with_final_result()
            {
                var runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteFinished(It.IsAny<TestCaseSummary>()));
            }

            [Fact]
            public void Will_save_compiler_cache_after_test_run()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                runner.Mock<ICompilerCache>().Verify(x => x.Save());
            }


            [Fact]
            public void Will_clean_up_test_context()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

               runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context));
            }

            [Fact]
            public void Will_not_clean_up_test_context_if_debug_mode()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                runner.ClassUnderTest.DebugEnabled = true;

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context), Times.Never());
            }

            [Fact]
            public void Will_not_clean_up_test_context_if_open_in_browser_is_set()
            {
                var runner = new TestableTestRunner();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions{ OpenInBrowser = true});

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context), Times.Never());
            }

            [Fact]
            public void Will_call_exception_thrown_on_callback_and_move_to_next_test_file()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var exception = new Exception();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "testRunner.js" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<PathInfo>(f => f.FullPath == @"path\tests1.html"), It.IsAny<TestOptions>(), out context)).Throws(exception);
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<PathInfo>(f => f.FullPath == @"path\tests2.html"), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests1.html")).Throws(exception);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests2.html")).Returns(@"D:\path\tests2.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                var args = TestableTestRunner.BuildArgs("runner.js", "file:///D:/harnessPath.html", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                 .Returns(new ProcessResult<TestFileSummary>(0, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests1.html", @"path\tests2.html" }, testCallback.Object);

                testCallback.Verify(x => x.ExceptionThrown(exception, @"path\tests1.html"));
                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_record_timeout_exception_from_test_runner()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "testRunner.js" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns(TestRunner.HeadlessBrowserName);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(TestRunner.HeadlessBrowserName, TestableTestRunner.ExecutionPhantomArgs, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>((int)TestProcessExitCode.Timeout, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html"}, testCallback.Object);

                testCallback.Verify(x => x.FileError(It.IsAny<TestError>()));
                Assert.Equal(1, res.TotalCount);
                Assert.Equal(1, res.Errors.Count);
            }

            [Fact]
            public void Will_record_unknown_exception_from_test_runner()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var context = new TestContext { TestHarnessPath = @"D:\harnessPath.html", TestRunner = "testRunner.js" };
                runner.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<PathInfo>(), It.IsAny<TestOptions>(), out context)).Returns(true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns(TestRunner.HeadlessBrowserName);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(TestRunner.HeadlessBrowserName, TestableTestRunner.ExecutionPhantomArgs, It.IsAny<Func<ProcessStream, TestFileSummary>>()))
                    .Returns(new ProcessResult<TestFileSummary>((int)TestProcessExitCode.Unknown, summary));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html" }, testCallback.Object);

                testCallback.Verify(x => x.FileError(It.IsAny<TestError>()));
                Assert.Equal(1, res.TotalCount);
                Assert.Equal(1, res.Errors.Count);
            }
        }
    }
}