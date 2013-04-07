using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Moq;
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
                        new object[] {@"JS\Test\references-qunit.js"},
                        new object[] {@"JS\Test\references-jasmine.js"}
                    };
            }
        }

        public static IEnumerable<object[]> CoffeeScriptTests
        {
            get
            {
                return new[]
                    {
                        new object[] {@"JS\Test\basic-qunit.coffee"},
                        new object[] {@"JS\Test\basic-jasmine.coffee"}
                    };
            }
        }


        public static IEnumerable<object[]> BasicTestScripts
        {
            get
            {
                return new[]
                    {
                        new object[] {@"JS\Test\basic-qunit.js"},
                        new object[] {@"JS\Test\basic-jasmine.js"}
                    };
            }
        }

        public Execution()
        {
            // Disable caching
            GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes = 0;
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_run_tests_from_a_js_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_not_record_stack_trace_for_failed_jasmine_expectations()
        {
            var testRunner = TestRunner.Create();

            testRunner.DebugEnabled = true;
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\failing-expectation-jasmine.js", new ExceptionThrowingRunnerCallback());

            Assert.Null(result.Tests.Single().TestResults.Single().StackTrace);
        }

        [Fact]
        public void Will_record_stack_trace_of_exception_thrown_in_code()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\jasmine-scriptError.js", new ExceptionThrowingRunnerCallback());

            var stackTrace = result.Tests.Single().TestResults.Single().StackTrace;
            Assert.NotNull(stackTrace);
            Assert.Contains("/jasmine-scriptError.js:5", stackTrace);
        }

        [Fact]
        public void Will_strip_message_line_from_stack_trace_of_exception_thrown_in_code()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\jasmine-scriptError.js", new ExceptionThrowingRunnerCallback());

            var stackTrace = result.Tests.Single().TestResults.Single().StackTrace;
            Assert.NotNull(stackTrace);
            Assert.DoesNotContain("Error: CODE ERROR", stackTrace);
        }

        [Theory]
        [PropertyData("CoffeeScriptTests")]
        public void Will_run_tests_from_a_coffee_script_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }
        
        [Fact]
        public void Will_autodetect_qunit_when_using_fully_qualified_function_names()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\fullyQualifiedQunit.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }
        
        [Fact]
        public void Will_follow_references_using_chutzpah_reference_syntax()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\chutzpahReferenceComment.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_process_type_script_files_together()
        {
            // This test verifies that we run TypeScript compiler on all ts files at once 
            // this is important since TS using typechecking engine to help generate code
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\TypeScript\test.ts", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_QUnit_tests_even_when_QUnit_included_twice()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\qunitTwice.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Theory]
        [PropertyData("ReferencesTestScripts")]
        public void Will_expand_references_in_a_js_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_folder()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\SubFolder", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_run_tests_from_a_folder_and_a_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result =
                testRunner.RunTests(new List<string> {@"JS\Test\basic-qunit.js", @"JS\Test\SubFolder"},
                                    new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(5, result.PassedCount);
            Assert.Equal(6, result.TotalCount);
        }

        [Fact]
        public void Will_run_tests_with_dependencies_having_the_same_name()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\sameName.js" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }


        [Fact]
        public void Will_copy_over_files_from_a_referenced_folders()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\folderReference.js" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_copy_over_css_references_from_js_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\styleReference.js" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_copy_over_css_references_from_html_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\styleReference.html" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }
		
		[Fact]
        public void Will_not_copy_over_excluded_reference_from_js_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\excludeReference.js" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_execute_async_test()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\asyncTest.js" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact(Timeout = 4000)]
        public void Will_execute_nothing_if_test_takes_longer_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> {@"JS\Test\timeoutTest.js"}, new TestOptions {TestFileTimeoutMilliseconds = 500});

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact(Timeout = 4000)]
        public void Will_execute_nothing_if_test_file_has_infinite_loop()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> {@"JS\Test\spinWait.js"}, new TestOptions {TestFileTimeoutMilliseconds = 500});

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public void Will_execute_test_if_test_takes_less_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> {@"JS\Test\timeoutTest.js"},
                                                         new TestOptions {TestFileTimeoutMilliseconds = 1500},
                                                         new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_without_module()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js", new ExceptionThrowingRunnerCallback());

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("A basic test"));
            Assert.Equal(3, test.Line);
            Assert.Equal(2, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_with_module()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js", new ExceptionThrowingRunnerCallback());

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(11, test.Line);
            Assert.Equal(3, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_jasmine_test()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-jasmine.js", new ExceptionThrowingRunnerCallback());

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(13, test.Line);
            Assert.Equal(5, test.Column);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_html_file()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.html", new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_pass_qunit_tests_that_depend_on_fixture_from_source_test_harness()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\fixture.html", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_with_characters_that_need_encoding()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\encoding.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_coffee_script_test_that_asserts_unicode_characters()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\coffeeEncoding.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_that_has_a_reference_to_web_url()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\webReference.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_passing_tests_that_has_a_reference_to_https_web_url()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\webReferenceSSL.js", new ExceptionThrowingRunnerCallback());

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
            TestCaseSummary result = testRunner.RunTests(tests, new ExceptionThrowingRunnerCallback());

            Assert.Equal(2, result.FailedCount);
            Assert.Equal(6, result.PassedCount);
            Assert.Equal(8, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_log()
        {
            var testRunner = TestRunner.Create();

            testRunner.DebugEnabled = true;
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleLog.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_jasmine_log()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\jasmineLog.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_capture_message_logged_via_console_log()
        {
            var testRunner = TestRunner.Create();

            testRunner.DebugEnabled = true;
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleLog.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal("hello", result.Logs.Single().Message);
        }

        [Fact]
        public void Will_capture_message_logged_via_jasmine_log()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\jasmineLog.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal("hello", result.Logs.Single().Message);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_error()
        {
            var testRunner = TestRunner.Create();
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleError.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_logs_object_to_console_warn()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleWarn.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_test_which_has_script_error_which_gets_logged_to_output()
        {
            var testRunner = TestRunner.Create();
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\scriptError.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(3, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_get_correct_module_name_for_nested_jasmine_suites()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\nestedJasmine.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal("nested.jasmine hello", result.Tests.First().ModuleName);
            Assert.Equal(1, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_execute_ajax_call_test()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(new List<string> { @"JS\Test\ajaxCall.js" }, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_tests_from_a_html_file_when_using_old_version_of_qunit()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\oldCallbackQUnit.html", new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_run_qunit_html_file_with_inline_tests()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\inlineTests.html", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_run_jasmine_test_which_uses_iit()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\jasmine-iit.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(3, result.TotalCount);
        }

        [Fact]
        public void Will_run_jasmine_test_which_uses_ddescribe()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\jasmine-ddescribe.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(3, result.TotalCount);
        }

        [Fact]
        public void Will_report_a_failed_CoffeeScript_compilation_to_the_callback()
        {
            var testRunner = TestRunner.Create();
            var callback = new Mock<ITestMethodRunnerCallback>();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\syntaxError.coffee", callback.Object);

            callback.Verify(x => x.ExceptionThrown(
                It.Is((ChutzpahException ex) => ex.Message.Contains("Unexpected '->'")),
                It.Is((string s) => s.Contains("syntaxError.coffee"))
                ));
        }

        [Fact]
        public void Will_pinpoint_the_correct_file_in_the_exception_when_CoffeeScript_compilation_fails()
        {
            var testRunner = TestRunner.Create();
            var callback = new Mock<ITestMethodRunnerCallback>();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\includeFileWithSyntaxError.coffee", callback.Object);

            callback.Verify(x => x.ExceptionThrown(
                It.Is((ChutzpahException ex) => ex.ToString().Contains("\\syntaxError.coffee")),
                It.Is((string s) => s.Contains("includeFileWithSyntaxError.coffee"))
                ));
        }

        [Fact]
        public void Will_strip_unnecessary_info_when_reporting_a_failed_CoffeeScript_compilation_to_the_callback()
        {
            var testRunner = TestRunner.Create();
            var callback = new Mock<ITestMethodRunnerCallback>();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\syntaxError.coffee", callback.Object);

            callback.Verify(x => x.ExceptionThrown(
                It.Is((ChutzpahException ex) => !ex.Message.Contains("Microsoft JScript runtime error") &&
                                                !ex.Message.Contains("Error Code") &&
                                                !ex.Message.Contains("Error WCode") &&
                                                !Regex.IsMatch(ex.Message, "^at line", RegexOptions.Multiline)
                    ),
                It.IsAny<string>()
                                     ));
        }

        public class TypeScript
        {
            public static IEnumerable<object[]> TypeScriptTests
            {
                get
                {
                    return new[]
                        {
                            new object[] {@"JS\Test\basic-qunit.ts"},
                            new object[] {@"JS\Test\basic-jasmine.ts"}
                        };
                }
            }

            [Theory]
            [PropertyData("TypeScriptTests")]
            public void Will_run_tests_from_a_type_script_file(string scriptPath)
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

                Assert.Equal(1, result.FailedCount);
                Assert.Equal(3, result.PassedCount);
                Assert.Equal(4, result.TotalCount);
            }

            [Fact]
            public void Will_convert_ES5_code_given_setting()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TypeScript\ES5\ES5Test.ts", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void Will_report_a_failed_TypeScript_compilation_to_the_callback()
            {
                var testRunner = TestRunner.Create();
                var callback = new Mock<ITestMethodRunnerCallback>();

                TestCaseSummary result = testRunner.RunTests(@"JS\Test\syntaxError.ts", callback.Object);

                callback.Verify(x => x.ExceptionThrown(
                    It.Is((ChutzpahException ex) => ex.Message.Contains("Expected ';'")),
                    It.Is((string s) => s.Contains("syntaxError.ts"))
                    ));
            }
        }

        public class AMD
        {
            [Fact]
            public void Will_run_qunit_require_html_test_where_test_file_uses_requirejs_command()
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(@"JS\Test\requirejs\qunit-test.html", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }

            [Fact]
            public void Will_run_jasmine_require_html_test_where_test_file_uses_requirejs_command()
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(@"JS\Test\requirejs\jasmine-test.html", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }

            [Fact]
            public void Will_run_qunit_require_js_test_where_test_file_uses_requirejs_command()
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(@"JS\Code\RequireJS\all.tests.qunit.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }

            [Fact]
            public void Will_run_jasmine_require_js_test_where_test_file_uses_requirejs_command()
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(@"JS\Code\RequireJS\all.tests.jasmine.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }

            [Fact]
            public void Will_run_qunit_require_js_test_using_settings_to_place_harness()
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(@"JS\Test\requirejs\WithSettings\rjs-qunit-solo.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }

            [Fact]
            public void Will_run_jasmine_require_js_test_using_settings_to_place_harness()
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(@"JS\Test\requirejs\WithSettings\rjs-jasmine-solo.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }
        }

        public class ChutzpahSettingsFile
        {
            [Fact]
            public void Will_use_settings_file_to_determine_framework()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\frameworkTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void Can_place_test_harness_next_to_settings_file()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\Sub1\settingsAdjacentHarnessTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void Can_place_test_harness_next_to_test_file()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\Sub2\SubSub2\testAdjacentHarnessTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void Can_place_test_harness_in_custom_path()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\Sub3\customPathHarnessTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void Will_set_root_reference_path_to_settings_dir()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\RootReferencePathModeTests\settingsFileDirectoryTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }
        }
    }
}