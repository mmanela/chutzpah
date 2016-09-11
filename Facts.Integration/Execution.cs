using System;
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
                        new object[] {@"JS\Test\references-qunit.js"}
                    };
            }
        }

        public static IEnumerable<object[]> HtmlTemplateTestScripts
        {
            get
            {
                return new[]
                    {
                        new object[] {@"JS\Test\HtmlTemplate\template-qunit.js"}
                    };
            }
        }

        public static IEnumerable<object[]> BasicTestScripts
        {
            get
            {
                return new[]
                    {
                        new object[] { @"JS\Test\basic-qunit.js", null },
                        new object[] { @"JS\Test\basic-jasmine.js", "1" },
                        new object[] { @"JS\Test\basic-jasmine.js", "2" },
                        new object[] {@"JS\Test\basic-mocha-bdd.js", null }, 
                        new object[] {@"JS\Test\basic-mocha-tdd.js", null},
                        new object[] {@"JS\Test\basic-mocha-qunit.js", null}
                    };
            }
        }

        public static IEnumerable<object[]> ChutzpahSamples
        {
            get { return TestPathGroups.ChutzpahSamples; }
        }

        public static IEnumerable<object[]> SkippedTests
        {
            get { return TestPathGroups.SkippedTests; }
        }

        public static IEnumerable<object[]> ManualStartAmdTests
        {
            get { return TestPathGroups.ManualStartAmdTests; }
        }

        
        public Execution()
        {
            ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
        }

        [Theory]
        [MemberData("ChutzpahSamples")]
        public void Will_run_tests_from_chutzpah_samples(string scriptPath, int count)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(count, result.PassedCount);
            Assert.Equal(count, result.TotalCount);
        }

        [Theory]
        [MemberData("ManualStartAmdTests")]
        public void Will_run_manual_start_amd_tests(string scriptPath, int count)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(count, result.PassedCount);
            Assert.Equal(count, result.TotalCount);
        }


        [Theory]
        [MemberData("BasicTestScripts")]
        public void Will_run_tests_from_a_js_file(string scriptPath, string frameworkVersion)
        {
            var testRunner = TestRunner.Create();
            ChutzpahTestSettingsFile.Default.FrameworkVersion = frameworkVersion;

            TestCaseSummary result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(3, result.PassedCount);
            Assert.Equal(4, result.TotalCount);
        }

        [Theory]
        [MemberData("SkippedTests")]
        public void Will_report_skipped_tests(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(1, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(3, result.TotalCount);
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

        [Fact]
        public void Will_run_tests_with_unicode_characters()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(
                @"JS\Test\basic-unicode.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Polish special chars: ąćęłńóśżź", result.Tests.First().TestName);
        }

        [Fact]
        public void Will_run_tests_using_custom_test_harness_path()
        {
            var testRunner = TestRunner.Create();
            TestCaseSummary result = testRunner.RunTests(@"JS\Test\TestSettings\CustomTestHarness\customHarnessTest.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.PassedCount);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_encode_file_path_of_test_file_with_hash_tag()
        {
            if (ChutzpahTestSettingsFile.ForceWebServerMode) { return; }
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\PathEncoding\C#\pathEncoding.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_encode_file_path_of_test_file_with_space()
        {
            if (ChutzpahTestSettingsFile.ForceWebServerMode) { return; }
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\PathEncoding\With Space+\pathEncoding.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
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
        public void Will_run_QUnit_tests_even_when_QUnit_included_twice()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\qunitTwice.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Theory]
        [MemberData("ReferencesTestScripts")]
        public void Will_expand_references_in_a_js_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(scriptPath, new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Theory]
        [MemberData("HtmlTemplateTestScripts")]
        public void Will_load_html_template(string scriptPath)
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
                testRunner.RunTests(new List<string> { @"JS\Test\basic-qunit.js", @"JS\Test\SubFolder" },
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

        [Fact]
        public void Will_execute_nothing_if_test_takes_longer_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\timeoutTest.js" }, new TestOptions { TestFileTimeoutMilliseconds = 500 });

            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);

        }

        [Fact]
        public void Will_execute_nothing_if_test_takes_longer_than_timeout_from_settings_file()
        {
            var testRunner = TestRunner.Create();

            // The time out from test options will get overridden by the one from the file
            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\TimeoutSetting\timeoutTest.js" }, new TestOptions { TestFileTimeoutMilliseconds = 3500 });

            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public void Will_execute_nothing_if_test_file_has_infinite_loop()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\spinWait.js" }, new TestOptions { TestFileTimeoutMilliseconds = 500 });

            Assert.Equal(1, result.Errors.Count);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(0, result.PassedCount);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public void Will_execute_test_if_test_takes_less_than_timeout()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(new List<string> { @"JS\Test\timeoutTest.js" },
                                                         new TestOptions { TestFileTimeoutMilliseconds = 10000 },
                                                         new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.FailedCount);
            Assert.Equal(1, result.PassedCount);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_with_module()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-qunit.js", new ExceptionThrowingRunnerCallback());

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(11, test.Line);
            Assert.Equal(9, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_jasmine_test()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\basic-jasmine.js", new ExceptionThrowingRunnerCallback());

            var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(13, test.Line);
            Assert.Equal(9, test.Column);
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
        public void Will_run_multiple_js_files_and_aggregate_results()
        {
            var testRunner = TestRunner.Create();
            ChutzpahTracer.Enabled = true;
            ChutzpahTracer.AddConsoleListener();
            var tests = new List<string>
                {
                    @"JS\Test\basic-qunit.js",
                    @"JS\Test\basic-jasmine.js"
                };
            TestCaseSummary result = testRunner.RunTests(tests, new ExceptionThrowingRunnerCallback());

            Assert.Equal(2, result.FailedCount);
            Assert.Equal(6, result.PassedCount);
            Assert.Equal(8, result.TotalCount);
        }

        [Fact]
        public void Will_run_multiple_files_and_aggregate_results()
        {
            var testRunner = TestRunner.Create();

            ChutzpahTracer.Enabled = true;
            ChutzpahTracer.AddConsoleListener();
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
        public void Will_capture_message_logged_via_console_log()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = testRunner.RunTests(@"JS\Test\consoleLog.js", new ExceptionThrowingRunnerCallback());

            Assert.Equal("hello", result.Logs.Single().Message);
        }

        [Fact]
        public void Will_capture_message_logged_via_jasmine_log()
        {
            var testRunner = TestRunner.Create();

            TestCaseSummary result = null;
            TestUtils.RunAsJasmineVersionOne(() =>
            {
                result = testRunner.RunTests(@"JS\Test\jasmineLog.js", new ExceptionThrowingRunnerCallback());
            });

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


        public class TestFileBatching
        {
            public TestFileBatching()
            {
                ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
            }

            [Fact]
            public void Will_place_ambiguous_tests_correctly_when_file_contains_unambiguous_tests()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\Batching\Ambiguous1\chutzpah.json", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(6, result.PassedCount);
                Assert.Equal(6, result.TotalCount);
                foreach (var file in result.TestFileSummaries)
                {
                    Assert.Equal(2, file.Tests.Count());
                }
            }

        }

        public class AMD
        {
            public static IEnumerable<object[]> AmdTestScriptWithForcedRequire
            {
                get
                {
                    return new[]
                    {
                        new object[] {@"JS\Code\RequireJS\all.tests.qunit.js"},
                    };
                }
            }

            public static IEnumerable<object[]> AmdTestScriptWithAMDMode
            {
                get
                {
                    return new[]
                    {
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\base\base.qunit.test.js"},
                    };
                }
            }


            public AMD()
            {
                ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
            }


            [Theory]
            [MemberData("AmdTestScriptWithAMDMode")]
            public void Will_run_requirejs_tests_with_chutzpah_in_amd_mode(string path)
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(path, new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Theory]
            [MemberData("AmdTestScriptWithForcedRequire")]
            public void Will_run_require_js_test_where_test_file_uses_requirejs_command(string path)
            {
                var testRunner = TestRunner.Create();

                TestCaseSummary result = testRunner.RunTests(path, new ExceptionThrowingRunnerCallback());

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
        }

        public class ChutzpahSettingsFile
        {
            public ChutzpahSettingsFile()
            {
                ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
            }

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

            [Fact]
            public void Will_include_reference_from_settings_file()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\ReferencePaths\ReferencesTests.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(2, result.PassedCount);
                Assert.Equal(2, result.TotalCount);
            }

            [Fact]
            public void will_use_tests_setting_to_filter_tests()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\TestPaths", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(4, result.PassedCount);
                Assert.Equal(4, result.TotalCount);
            }

            [Fact]
            public void will_use_tests_setting_with_multiple_include_excludes_to_filter_tests()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\TestPathsMultipleIncludeExclude", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(4, result.PassedCount);
                Assert.Equal(4, result.TotalCount);
            }

            [Fact]
            public void will_use_tests_setting_to_discover_tests()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\TestPaths\chutzpah.json", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(4, result.PassedCount);
                Assert.Equal(4, result.TotalCount);
            }

            [Fact]
            public void will_use_settings_for_jasmine_version_one()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\FrameworkVersion\Jasmine_V1\versionTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void will_use_settings_for_jasmine_version_two()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\FrameworkVersion\Jasmine_V2\versionTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }

            [Fact]
            public void Will_use_testpattern_setting()
            {
                var testRunner = TestRunner.Create();
                var result = testRunner.RunTests(@"JS\Test\TestSettings\TestPattern\testPattern.js", new ExceptionThrowingRunnerCallback());

                var test = result.Tests.SingleOrDefault(x => x.TestName.Equals("Pattern 1"));
                Assert.Equal(20, test.Line);
                Assert.Equal(16, test.Column);

                test = result.Tests.SingleOrDefault(x => x.TestName.Equals("Pattern 2"));
                Assert.Equal(24, test.Line);
                Assert.Equal(24, test.Column);
            }

            [Fact]
            public void Will_set_user_agent()
            {
                var testRunner = TestRunner.Create();

                var result = testRunner.RunTests(@"JS\Test\TestSettings\UserAgent\userAgentTest.js", new ExceptionThrowingRunnerCallback());

                Assert.Equal(0, result.FailedCount);
                Assert.Equal(1, result.PassedCount);
                Assert.Equal(1, result.TotalCount);
            }
        }
    }
}