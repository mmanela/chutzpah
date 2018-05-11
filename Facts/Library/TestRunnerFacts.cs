using Chutzpah.Facts.Mocks;
using Chutzpah.Models;
using Chutzpah.Transformers;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestRunnerFacts
    {
        private class TestableTestRunner : Testable<TestRunner>
        {
            public static string ExecutionPhantomArgs = BuildArgs("runner.js", "harnessPath", "execution", Constants.DefaultTestFileTimeout);

            public TestableTestRunner()
            {
                InjectArray(new[] { Mock<ITestExecutionProvider>() });
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns("");
                Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(It.IsAny<IEnumerable<string>>()))
                    .Returns<IEnumerable<string>>((x) => x.Select(f => new PathInfo { Path = f, FullPath = f }));

                Mock<IChutzpahTestSettingsService>()
                    .Setup(x => x.FindSettingsFile(It.IsAny<string>(), It.IsAny<ChutzpahSettingsFileEnvironments>()))
                    .Returns(new ChutzpahTestSettingsFile().InheritFromDefault());

                Mock<IFileProbe>()
                .Setup(x => x.BuiltInDependencyDirectory)
                .Returns(@"dependencyPath\");

                Mock<IUrlBuilder>()
                    .Setup(x => x.GenerateFileUrl(It.IsAny<TestContext>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                    .Returns<TestContext, string, bool, bool, string>((c, p, fq, d, s) => p);

                Mock<ITestExecutionProvider>()
                    .Setup(x => x.CanHandleBrowser(It.IsAny<Engine>())).Returns(true);
                Mock<ITestExecutionProvider>()
                    .Setup(x => x.Execute(It.IsAny<TestOptions>(), It.IsAny<TestContext>(), TestExecutionMode.Execution, It.IsAny<ITestMethodRunnerCallback>()))
                    .Returns(new List<TestFileSummary> { new TestFileSummary("somePath") });
            }

            public static string BuildArgs(string runner, string harness, string mode = "execution", int? timeout = null, bool ignoreResourceLoadingError = false)
            {
                var format = "{0} \"{1}\" \"{2}\" {3} {4} {5} ";
                return string.Format(format, "--ignore-ssl-errors=true --proxy-type=none --ssl-protocol=any", runner, harness, mode, timeout.HasValue ? timeout.ToString() : "", ignoreResourceLoadingError.ToString());
            }

            public TestContext SetupTestContext(string[] testPaths = null, string harnessPath = @"harnessPath", string testRunnerPath = "testRunner.js", bool success = true, bool @throw = false, Engine browser = Engine.Phantom)
            {
                var context = new TestContext { TestHarnessPath = harnessPath, TestRunner = testRunnerPath };
                context.TestFileSettings.Engine = browser;
                if (testPaths != null)
                {
                    var pathCount = testPaths.Length;
                    if (@throw)
                    {
                        this.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<IEnumerable<PathInfo>>(fs => fs.Select(f => f.FullPath).Intersect(testPaths).Count() == pathCount), It.IsAny<TestOptions>(), out context)).Throws(new Exception());
                    }
                    else
                    {
                        this.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.Is<IEnumerable<PathInfo>>(fs => fs.Select(f => f.FullPath).Intersect(testPaths).Count() == pathCount), It.IsAny<TestOptions>(), out context)).Returns(success);

                    }
                }
                else
                {
                    if (@throw)
                    {
                        this.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<IEnumerable<PathInfo>>(), It.IsAny<TestOptions>(), out context)).Throws(new Exception());
                    }
                    else
                    {
                        this.Mock<ITestContextBuilder>().Setup(x => x.TryBuildContext(It.IsAny<IEnumerable<PathInfo>>(), It.IsAny<TestOptions>(), out context)).Returns(success);
                    }
                }

                return context;
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
                runner.Mock<ITestContextBuilder>().Setup(x => x.IsTestFile("a.js", null)).Returns(true);

                var result = runner.ClassUnderTest.IsTestFile("a.js", null);

                Assert.True(result);
            }

            [Fact]
            public void Will_false_if_not_test_file()
            {
                var runner = new TestableTestRunner();
                runner.Mock<ITestContextBuilder>().Setup(x => x.IsTestFile("a.js", null)).Returns(false);

                var result = runner.ClassUnderTest.IsTestFile("a.js", null);

                Assert.False(result);
            }
        }

        public class DiscoverTests
        {
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
                var context = runner.SetupTestContext(testPaths: new[] { @"path\tests.html" }, harnessPath: @"harnessPath", testRunnerPath: "runner");
                runner.Mock<ITestExecutionProvider>()
                    .Setup(x => x.Execute(It.IsAny<TestOptions>(), It.IsAny<TestContext>(), TestExecutionMode.Discovery, It.IsAny<ITestMethodRunnerCallback>()))
                    .Returns(new List<TestFileSummary> { summary });

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
                var context = runner.SetupTestContext(testPaths: new[] { @"path\tests.html" }, harnessPath: @"harnessPath", testRunnerPath: "runner");
                runner.Mock<ITestExecutionProvider>()
                    .Setup(x => x.Execute(It.IsAny<TestOptions>(), It.IsAny<TestContext>(), TestExecutionMode.Execution, It.IsAny<ITestMethodRunnerCallback>()))
                    .Returns(new List<TestFileSummary> { summary });

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_open_test_file_in_browser_when_given_flag()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext(testRunnerPath: "testRunner.js");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestLaunchMode = TestLaunchMode.FullBrowser });

                runner.Mock<IProcessHelper>().Verify(x => x.LaunchFileInBrowser(It.IsAny<TestContext>(), @"harnessPath", It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()));
            }

            [Fact]
            public void Will_call_test_suite_started()
            {
                var runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = runner.SetupTestContext();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteStarted());
            }

            [Fact]
            public void Will_call_test_suite_finished_with_final_result()
            {
                var runner = new TestableTestRunner();
                var testCallback = new MockTestMethodRunnerCallback();
                var context = runner.SetupTestContext();

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteFinished(It.IsAny<TestCaseSummary>()));
            }


            [Fact]
            public void Will_clean_up_test_context()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext();

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context));
            }

            [Fact]
            public void Will_not_clean_up_test_context_if_debug_mode()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext();
                runner.ClassUnderTest.EnableDebugMode();

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context), Times.Never());
            }

            [Fact]
            public void Will_not_clean_up_test_context_if_open_in_browser_is_set()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext();

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestLaunchMode = TestLaunchMode.FullBrowser });

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context), Times.Never());
            }

            [Fact]
            public void Will_call_process_transforms()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var context = runner.SetupTestContext(testRunnerPath: "testRunner.js");
                runner.Mock<ITestExecutionProvider>()
                    .Setup(x => x.Execute(It.IsAny<TestOptions>(), It.IsAny<TestContext>(), TestExecutionMode.Execution, It.IsAny<ITestMethodRunnerCallback>()))
                    .Returns(new List<TestFileSummary> { summary });

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html" }, testCallback.Object);

                runner.Mock<ITransformProcessor>().Verify(x => x.ProcessTransforms(It.Is<IEnumerable<TestContext>>(c => c.Count() == 1 && c.Single() == context), res));
            }

            private ChutzpahTestSettingsFile GetTransformTestSettings(string path)
            {
                return new ChutzpahTestSettingsFile
                {
                    Transforms = new List<TransformConfig>
                    {
                        new TransformConfig { Name = "mock", Path = path }
                    }
                }.InheritFromDefault();
            }
        }
    }
}