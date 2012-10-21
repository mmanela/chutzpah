using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    public class Execution
    {
        public static IEnumerable<object[]> ReferencesTestScripts
        {
            get
            {
                return new[]
                {
                    new object[] { @"JS\Test\references-qunit.js" },
                    new object[] { @"JS\Test\references-jasmine.js" }
                };
            }
        }

        public static IEnumerable<object[]> CoffeeScriptTests
        {
            get
            {
                return new[]
                {
                    new object[] { @"JS\Test\basic-qunit.coffee" },
                    new object[] { @"JS\Test\basic-jasmine.coffee" }
                };
            }
        }


        public static IEnumerable<object[]> TypeScriptTests
        {
            get
            {
                return new[]
                {
                    new object[] { @"JS\Test\basic-qunit.ts" },
                    new object[] { @"JS\Test\basic-jasmine.ts" }
                };
            }
        }

        public static IEnumerable<object[]> BasicTestScripts
        {
            get
            {
                return new[]
                {
                   new object[] { @"JS\Test\basic-qunit.js" },
                   new object[] { @"JS\Test\basic-jasmine.js" }
                };
            }
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_run_tests_from_a_js_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(scriptPath);

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Theory]
        [PropertyData("CoffeeScriptTests")]
        public void Will_run_tests_from_a_coffee_script_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath);

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Theory]
        [PropertyData("TypeScriptTests")]
        public void Will_run_tests_from_a_type_script_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath);

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Theory]
        [PropertyData("ReferencesTestScripts")]
        public void Will_expand_references_in_a_js_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(scriptPath);

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_folder()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\SubFolder");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_run_tests_from_a_folder_and_a_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\basic-qunit.js", @"JS\Test\SubFolder" });

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(5, result.PassedCount);
            Assert.Equal(6, result.TotalCount);
        }

        [Fact]
        public void Will_run_tests_with_dependencies_having_the_same_name()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\sameName.js" });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }


        [Fact]
        public void Will_copy_over_files_from_a_referenced_folders()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\folderReference.js" });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_copy_over_css_references_from_js_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\styleReference.js" });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_copy_over_css_references_from_html_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\styleReference.html" });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_execute_async_test()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\asyncTest.js" });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact(Timeout = 4000)]
        public void Will_execute_nothing_if_test_takes_longer_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\timeoutTest.js" }, new TestOptions { TestFileTimeoutMilliseconds = 500 });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact(Timeout = 4000)]
        public void Will_execute_nothing_if_test_file_has_infinite_loop()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\spinWait.js" }, new TestOptions { TestFileTimeoutMilliseconds = 500 });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public void Will_execute_test_if_test_takes_less_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\timeoutTest.js" }, new TestOptions { TestFileTimeoutMilliseconds = 1500 });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_without_module()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js");

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("A basic test"));
            Assert.Equal(3, test.Line);
            Assert.Equal(2, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_with_module()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js");

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(11, test.Line);
            Assert.Equal(3, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_jasmine_test()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-jasmine.js");

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(13, test.Line);
            Assert.Equal(5, test.Column);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_html_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.html");

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_pass_qunit_tests_that_depend_on_fixture_from_source_test_harness()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\fixture.html");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_with_characters_that_need_encoding()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\encoding.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_coffee_script_test_that_asserts_unicode_characters()
        {
            var testRunner = TestRunner.Create();
            
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\coffeeEncoding.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_that_has_a_reference_to_web_url()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\webReference.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_that_has_a_reference_to_https_web_url()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\webReferenceSSL.js");

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
            TestCaseSummary result = testRunner.RunTests(tests);

            Assert.Equal(2, result.FailedCount);
            Assert.Equal(6, result.PassedCount);
            Assert.Equal(8, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_log()
        {
            var testRunner = TestRunner.Create();

            testRunner.DebugEnabled = true;
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleLog.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_error()
        {
            var testRunner = TestRunner.Create();
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleError.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_warn()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleWarn.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_has_script_error_which_gets_logged_to_output()
        {
            var testRunner = TestRunner.Create();
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\scriptError.js");

            Assert.Equal(3, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_get_correct_module_name_for_nested_jasmine_suites()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\nestedJasmine.js");

            Assert.Equal("nested.jasmine hello", result.Tests.First().ModuleName);
            Assert.Equal(1, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_require_html_test_where_test_file_uses_requirejs_command()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\requirejs\qunit-test.html");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_run_jasmine_require_html_test_where_test_file_uses_requirejs_command()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\requirejs\jasmine-test.html");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }


        [Fact]
        public void Will_run_qunit_require_js_test_where_test_file_uses_requirejs_command()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Code\RequireJS\all.tests.qunit.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_run_jasmine_require_js_test_where_test_file_uses_requirejs_command()
        {
            var testRunner = TestRunner.Create();
           
            TestCaseSummary result = testRunner.RunTests(@"JS\Code\RequireJS\all.tests.jasmine.js");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_execute_ajax_call_test()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(new List<string> { @"JS\Test\ajaxCall.js" });

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_html_file_when_using_old_version_of_qunit()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\oldCallbackQUnit.html");

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }
        
        [Fact]
        public void Will_run_qunit_html_file_with_inline_tests()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\inlineTests.html");

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }
    }
}