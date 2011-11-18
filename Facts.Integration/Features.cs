using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    public class Features
    {
        public static IEnumerable<object[]> TestScripts
        {
            get
            {
                return new object[][]
                {
                    new object[] { @"JS\Test\basic-qunit.js" },
                    new object[] { @"JS\Test\basic-jasmine.js" }
                };
            }
        }

        [Theory]
        [PropertyData("TestScripts")]
        public void Will_run_tests_from_a_js_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(scriptPath);

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_folder()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\SubFolder");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_run_tests_from_a_folder_and_a_file()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(new List<string> { @"JS\Test\basic-qunit.js", @"JS\Test\SubFolder" });

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(5, result.PassedCount);
            Assert.Equal(6, result.TotalCount);
        }


        [Fact]
        public void Will_execute_nothing_if_test_takes_longer_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(new List<string> { @"JS\Test\timeoutTest.js"}, new TestOptions{ TimeOutMilliseconds = 500});

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public void Will_execute_test_if_test_takes_less_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(new List<string> { @"JS\Test\timeoutTest.js" }, new TestOptions { TimeOutMilliseconds = 1500 });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_without_module()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js");

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("A basic test"));
            Assert.Equal(3, test.Line);
            Assert.Equal(2, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_with_module()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js");

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(11, test.Line);
            Assert.Equal(3, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_jasmine_test()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\basic-jasmine.js");

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(13, test.Line);
            Assert.Equal(5, test.Column);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_html_file()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.html");

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_pass_qunit_tests_that_depend_on_fixture_from_source_test_harness()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\fixture.html");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_with_characters_that_need_encoding()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\encoding.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_that_has_a_reference_to_web_url()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\webReference.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_multiple_files_and_aggregate_results()
        {
            var testRunner = TestRunner.Create();
            var tests = new List<string>
                            {
                                @"JS\Test\basic-qunit.js",
                                @"JS\Test\basic-qunit.html"
                            };
            TestResultsSummary result = testRunner.RunTests(tests);

            Assert.Equal(2, result.FailedCount);
            Assert.Equal(6, result.PassedCount);
            Assert.Equal(8, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_log()
        {
            var testRunner = TestRunner.Create();

            testRunner.DebugEnabled = true;
            TestResultsSummary result = testRunner.RunTests(@"JS\Test\consoleLog.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_error()
        {
            var testRunner = TestRunner.Create();
            TestResultsSummary result = testRunner.RunTests(@"JS\Test\consoleError.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_warn()
        {
            var testRunner = TestRunner.Create();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\consoleWarn.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_has_script_error_which_gets_logged_to_output()
        {
            var testRunner = TestRunner.Create();
            TestResultsSummary result = testRunner.RunTests(@"JS\Test\scriptError.js");

            Assert.Equal(3, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }
    }
}