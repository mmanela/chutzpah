using System.Collections.Generic;
using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Integration
{
    public class Features
    {
        [Fact]
        public void Will_run_a_tests_from_a_js_file()
        {
            var testRunner = new TestRunner();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\basic.js");

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_run_a_tests_from_a_html_file()
        {
            var testRunner = new TestRunner();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\basic.html");

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_run_a_passing_tests_with_characters_that_need_encoding()
        {
            var testRunner = new TestRunner();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\encoding.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_a_passing_tests_that_has_a_reference_to_web_url()
        {
            var testRunner = new TestRunner();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\webReference.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_multiple_files_and_aggregate_results()
        {
            var testRunner = new TestRunner();
            var tests = new List<string>
                            {
                                @"JS\Test\basic.js",
                                @"JS\Test\basic.html"
                            };
            TestResultsSummary result = testRunner.RunTests(tests);

            Assert.Equal(2, result.FailedCount);
            Assert.Equal(6, result.PassedCount);
            Assert.Equal(8, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_log()
        {
            var testRunner = new TestRunner();

            testRunner.DebugEnabled = true;
            TestResultsSummary result = testRunner.RunTests(@"JS\Test\consoleLog.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_error()
        {
            var testRunner = new TestRunner();
            TestResultsSummary result = testRunner.RunTests(@"JS\Test\consoleError.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_warn()
        {
            var testRunner = new TestRunner();

            TestResultsSummary result = testRunner.RunTests(@"JS\Test\consoleWarn.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_has_script_error_which_gets_logged_to_output()
        {
            var testRunner = new TestRunner();
            TestResultsSummary result = testRunner.RunTests(@"JS\Test\scriptError.js");

            Assert.Equal(3, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }
    }
}