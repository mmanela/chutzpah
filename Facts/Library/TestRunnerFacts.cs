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
                Mock<IFileProbe>().Setup(x => x.FindFilePath(It.IsAny<string>())).Returns("");
                Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(It.IsAny<IEnumerable<string>>()))
                    .Returns<IEnumerable<string>>((x) => x.Select(f => new PathInfo { Path = f, FullPath = f }));
                Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{new TestFileSummary("somePath")}));

                Mock<IChutzpahTestSettingsService>()
                    .Setup(x => x.FindSettingsFile(It.IsAny<string>(), It.IsAny<ChutzpahSettingsFileEnvironments>()))
                    .Returns(new ChutzpahTestSettingsFile().InheritFromDefault());

                Mock<IFileProbe>()
                .Setup(x => x.BuiltInDependencyDirectory)
                .Returns(@"dependencyPath\");

                Mock<IUrlBuilder>()
                    .Setup(x => x.GenerateFileUrl(It.IsAny<TestContext>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns<TestContext, string, bool, bool>((c, p, fq, d) => p);
            }

            public static string BuildArgs(string runner, string harness, string mode = "execution", int? timeout = null, bool ignoreResourceLoadingError = false)
            {
                var format = "{0} \"{1}\" \"{2}\" {3} {4} {5} ";
                return string.Format(format, "--ignore-ssl-errors=true --proxy-type=none --ssl-protocol=any", runner, harness, mode, timeout.HasValue ? timeout.ToString() : "", ignoreResourceLoadingError.ToString());
            }

            public TestContext SetupTestContext(string[] testPaths = null,  string harnessPath = @"harnessPath", string testRunnerPath = "testRunner.js", bool success = true, bool @throw = false)
            {
                var context = new TestContext { TestHarnessPath = harnessPath, TestRunner = testRunnerPath };
                if (testPaths != null)
                {
                    var pathCount = testPaths.Count();
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
                    if(@throw)
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
                var context = runner.SetupTestContext(testPaths: new []{@"path\tests.html"}, harnessPath: @"harnessPath", testRunnerPath: "runner");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "harnessPath", "discovery", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));

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
                var context = runner.SetupTestContext(testPaths: new []{@"path\tests.html"}, harnessPath: @"harnessPath", testRunnerPath: "runner");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "harnessPath", "execution", Constants.DefaultTestFileTimeout);

                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                Assert.Equal(1, res.TotalCount);
            }
            

            [Fact]
            public void Will_pass_timeout_option_to_test_runner()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = runner.SetupTestContext( testRunnerPath: "runner");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "harnessPath", "execution", 5000);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                 .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestFileTimeoutMilliseconds = 5000 });

                Assert.Equal(1, res.TotalCount);
            }
            
            
            [Fact]
            public void Will_use_timeout_from_context_if_exists()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = runner.SetupTestContext(testRunnerPath: "runner");
                context.TestFileSettings.TestFileTimeout = 6000;
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                var args = TestableTestRunner.BuildArgs("jsPath", "harnessPath", "execution", 6000);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                 .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary> { summary }));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestFileTimeoutMilliseconds = 5000 });

                Assert.Equal(1, res.TotalCount);
            }

            
            [Fact]
            public void Will_run_test_files_found_from_given_folder_path()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = runner.SetupTestContext( testRunnerPath: "runner");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\testFolder" }))
                    .Returns(new List<PathInfo> { new PathInfo { FullPath = @"path\tests.html" } });
                var args = TestableTestRunner.BuildArgs("jsPath", "harnessPath", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                 .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\testFolder");

                Assert.Equal(1, res.TotalCount);
            }

        
            [Fact]
            public void Will_run_test_files_found_from_chutzpah_json_files()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context = runner.SetupTestContext( testRunnerPath: "runner");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner")).Returns("jsPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\chutzpah.json")).Returns(@"D:\path\chutzpah.json");
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(It.IsAny<ChutzpahTestSettingsFile>()))
                    .Returns(new List<PathInfo> { new PathInfo { FullPath = @"path\tests.html" } });
                var args = TestableTestRunner.BuildArgs("jsPath", "harnessPath", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                 .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\chutzpah.json");

                
                Assert.Equal(1, res.TotalCount);
                runner.Mock<IFileProbe>()
                    .Verify(x => x.FindScriptFiles(It.IsAny<List<string>>()), Times.Never());
            }

            [Fact]
            public void Will_open_test_file_in_browser_when_given_flag()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext( testRunnerPath: "testRunner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestLaunchMode = TestLaunchMode.FullBrowser });

                runner.Mock<IProcessHelper>().Verify(x => x.LaunchFileInBrowser(It.IsAny<TestContext>(), @"harnessPath", It.IsAny<string>(), It.IsAny<IDictionary<string,string>>()));
            }

            [Fact]
            public void Will_run_multiple_test_files_and_return_results()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context1 = runner.SetupTestContext(harnessPath: @"harnessPath1", testRunnerPath: "runner1", testPaths: new []{@"path\tests1.html"});
                var context2 = runner.SetupTestContext(harnessPath: @"harnessPath2", testRunnerPath: "runner2", testPaths: new []{@"path\tests2.html"});
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests1.html")).Returns(@"D:\path\tests1.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests2.htm")).Returns(@"D:\path\tests2.htm");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner1")).Returns("jsPath1");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner2")).Returns("jsPath2");
                var args1 = TestableTestRunner.BuildArgs("jsPath1", "harnessPath1", "execution", Constants.DefaultTestFileTimeout);
                var args2 = TestableTestRunner.BuildArgs("jsPath2", "harnessPath2", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), args1, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), args2, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\tests1a.html", @"path\tests2a.htm" }))
                    .Returns(new List<PathInfo> { new PathInfo { FullPath = @"path\tests1.html" }, new PathInfo { FullPath = @"path\tests2.html" } });

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new List<string> { @"path\tests1a.html", @"path\tests2a.htm" });

                Assert.Equal(2, res.TotalCount);
            }

            [Fact]
            public void Will_batch_test_files_with_same_context_given_testFileBatching_setting_is_enabled()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var context1 = runner.SetupTestContext(harnessPath: @"harnessPath1", testRunnerPath: "runner1", testPaths: new[] { @"path\tests1.js", @"path\tests2.js" });
                var context2 = runner.SetupTestContext(harnessPath: @"harnessPath2", testRunnerPath: "runner2", testPaths: new[] { @"path2\tests1.js", @"path2\tests2.js" });
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner1")).Returns("jsPath1");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("runner2")).Returns("jsPath2");
                var args1 = TestableTestRunner.BuildArgs("jsPath1", "harnessPath1", "execution", Constants.DefaultTestFileTimeout);
                var args2 = TestableTestRunner.BuildArgs("jsPath2", "harnessPath2", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), args1, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary> { summary }));
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(It.IsAny<string>(), args2, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary> { summary }));
                runner.Mock<IFileProbe>()
                    .Setup(x => x.FindScriptFiles(new List<string> { @"path\tests1a.js", @"path\tests2a.js", @"path2\tests1a.js", @"path2\tests2a.js" }))
                    .Returns(new List<PathInfo> { 
                        new PathInfo { FullPath = @"path\tests1.js" }, 
                        new PathInfo { FullPath = @"path\tests2.js" } ,
                        new PathInfo { FullPath = @"path2\tests1.js" } ,
                        new PathInfo { FullPath = @"path2\tests2.js" } ,
                    });
                var settingsForPath = new ChutzpahTestSettingsFile { SettingsFileDirectory = "path", EnableTestFileBatching = true }.InheritFromDefault();
                var settingsForPath2 = new ChutzpahTestSettingsFile { SettingsFileDirectory = "path2", EnableTestFileBatching = true }.InheritFromDefault();
                runner.Mock<IChutzpahTestSettingsService>().Setup(x => x.FindSettingsFile(It.Is<string>(p => p.Contains(@"path\")), It.IsAny<ChutzpahSettingsFileEnvironments>())).Returns(settingsForPath);
                runner.Mock<IChutzpahTestSettingsService>().Setup(x => x.FindSettingsFile(It.Is<string>(p => p.Contains(@"path2\")), It.IsAny<ChutzpahSettingsFileEnvironments>())).Returns(settingsForPath2);

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new List<string> { @"path\tests1a.js", @"path\tests2a.js", @"path2\tests1a.js", @"path2\tests2a.js" });

                Assert.Equal(2, res.TotalCount);
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
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", testCallback.Object);

                testCallback.Verify(x => x.TestSuiteFinished(It.IsAny<TestCaseSummary>()));
            }


            [Fact]
            public void Will_clean_up_test_context()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context));
            }

            [Fact]
            public void Will_not_clean_up_test_context_if_debug_mode()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");
                runner.ClassUnderTest.EnableDebugMode();

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html");

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context), Times.Never());
            }

            [Fact]
            public void Will_not_clean_up_test_context_if_open_in_browser_is_set()
            {
                var runner = new TestableTestRunner();
                var context = runner.SetupTestContext();
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.TestRunnerJsName)).Returns("jsPath");

                TestCaseSummary res = runner.ClassUnderTest.RunTests(@"path\tests.html", new TestOptions { TestLaunchMode = TestLaunchMode.FullBrowser });

                runner.Mock<ITestContextBuilder>().Verify(x => x.CleanupContext(context), Times.Never());
            }

            [Fact]
            public void Will_add_exception_to_errors_and_move_to_next_test_file()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var exception = new Exception();
                var context = runner.SetupTestContext(testRunnerPath: "testRunner.js", testPaths: new []{@"path\tests1.html"});
                runner.SetupTestContext(testRunnerPath: "testRunner.js", testPaths: new []{@"path\tests2.html"}, @throw: true);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests1.html")).Throws(exception);
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests2.html")).Returns(@"D:\path\tests2.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns("browserPath");
                var args = TestableTestRunner.BuildArgs("runner.js", "harnessPath", "execution", Constants.DefaultTestFileTimeout);
                runner.Mock<IProcessHelper>()
                 .Setup(x => x.RunExecutableAndProcessOutput("browserPath", args, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                 .Returns(new ProcessResult<IList<TestFileSummary>>(0, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests1.html", @"path\tests2.html" }, testCallback.Object);

                testCallback.Verify(x => x.FileError(It.IsAny<TestError>()));
                Assert.Equal(1, res.Errors.Count);
                Assert.Equal(1, res.TotalCount);
            }

            [Fact]
            public void Will_record_timeout_exception_from_test_runner()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var context = runner.SetupTestContext( testRunnerPath: "testRunner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns(TestRunner.HeadlessBrowserName);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(TestRunner.HeadlessBrowserName, TestableTestRunner.ExecutionPhantomArgs, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>((int)TestProcessExitCode.Timeout, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html" }, testCallback.Object);

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
                var context = runner.SetupTestContext( testRunnerPath: "testRunner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns(TestRunner.HeadlessBrowserName);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(TestRunner.HeadlessBrowserName, TestableTestRunner.ExecutionPhantomArgs, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>((int)TestProcessExitCode.Unknown, new List<TestFileSummary>{summary}));

                TestCaseSummary res = runner.ClassUnderTest.RunTests(new[] { @"path\tests.html" }, testCallback.Object);

                testCallback.Verify(x => x.FileError(It.IsAny<TestError>()));
                Assert.Equal(1, res.TotalCount);
                Assert.Equal(1, res.Errors.Count);
            }

            [Fact]
            public void Will_call_process_transforms()
            {
                var runner = new TestableTestRunner();
                var summary = new TestFileSummary("somePath");
                summary.AddTestCase(new TestCase());
                var testCallback = new MockTestMethodRunnerCallback();
                var context = runner.SetupTestContext( testRunnerPath: "testRunner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(@"path\tests.html")).Returns(@"D:\path\tests.html");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath("testRunner.js")).Returns("runner.js");
                runner.Mock<IFileProbe>().Setup(x => x.FindFilePath(TestRunner.HeadlessBrowserName)).Returns(TestRunner.HeadlessBrowserName);
                runner.Mock<IProcessHelper>()
                    .Setup(x => x.RunExecutableAndProcessOutput(TestRunner.HeadlessBrowserName, TestableTestRunner.ExecutionPhantomArgs, It.IsAny<Func<ProcessStream, IList<TestFileSummary>>>()))
                    .Returns(new ProcessResult<IList<TestFileSummary>>((int)TestProcessExitCode.Unknown, new List<TestFileSummary>{summary}));

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