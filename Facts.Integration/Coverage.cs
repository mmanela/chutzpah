using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    public class Coverage
    {
        public Coverage()
        {
            ChutzpahTracer.Enabled = false;
        }

        private const string ABasicTestScript = @"JS\Test\basic-jasmine.js";
        private const string ACoffeeTestScript = @"JS\Test\basic-jasmine.coffee";

        public static IEnumerable<object[]> BasicTestScripts
        {
            get
            {
                return new[]
                {
                   new object[] { @"JS\Test\basic-qunit.js" },
                   new object[] { ABasicTestScript }
                };
            }
        }

        public static IEnumerable<object[]> RequireJsTestScripts
        {
            get
            {
                return new[]
                {
                    new object[] {@"JS\Code\RequireJS\all.tests.qunit.old.js"},
                    new object[] {@"JS\Code\RequireJS\all.tests.qunit.new.js"},
                    new object[] {@"JS\Code\RequireJS\all.tests.jasmine.old.js"},
                    new object[] {@"JS\Code\RequireJS\all.tests.jasmine.new.js"}
                };
            }
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_create_a_coverage_object(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObject);
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_cover_the_correct_scripts(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] {scriptPath, "JS\\Code\\code.js"});
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_get_test_results_with_coverage_enabled(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_get_coverage_for_path_with_hash_tag()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\PathEncoding\C#\pathEncoding.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\PathEncoding\C#\pathEncoding.js", "JS\\Code\\code.js" });
        }

        [Fact]
        public void Will_get_coverage_for_path_with_space()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\PathEncoding\With Space+\pathEncoding.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\PathEncoding\With Space+\pathEncoding.js", "JS\\Code\\code.js" });
        }

        [Fact]
        public void Will_include_files_from_settings_file()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\IncludePath\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] {  "JS\\Code\\code.js" });
        }

        [Fact]
        public void Will_exclude_files_from_settings_file()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\ExcludePath\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\Coverage\ExcludePath\test.js" });
        }

        [Fact]
        public void Will_honor_both_include_and_exclude_paths()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\IncludeExcludePath\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\Coverage\IncludeExcludePath\test.js" });
        }

        [Fact]
        public void Will_exclude_files_after_include_for_settings_file()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\CompetingIncludeExclude\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.Equal(0, result.TestFileSummaries.Single().CoverageObject.Count);
        }

        [Fact]
        public void Will_have_1_based_execution_count_lines()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var coverageLines = dict.Single(kvp => kvp.Key.Contains("code.js")).Value.LineExecutionCounts;
            Assert.Null(coverageLines[0]);
        }

        [Fact]
        public void Will_put_file_path_in_the_coverage_file_data()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var filePath = dict.Single(kvp => kvp.Key.Contains("code.js")).Value.FilePath;
            Assert.Contains("\\code.js", filePath);
        }

        [Fact]
        public void Will_create_empty_coverage_object_if_coverage_is_disabled()
        {
            var testRunner = TestRunner.Create(true);

            var result = testRunner.RunTests(ABasicTestScript, new ExceptionThrowingRunnerCallback());

            Assert.Empty(result.TestFileSummaries.Single().CoverageObject);
        }

        [Fact]
        public void Will_only_include_specified_files_in_coverage_report()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.IncludePatterns = new []{"**\\code.js"}),
                new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;
            ExpectKeysMatching(dict, new[] {"\\code.js"});
        }

        [Fact]
        public void Will_exclude_specified_files_from_the_coverage_report()
        {
            var testRunner = TestRunner.Create();
            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.ExcludePatterns = new[]{"**\\" + ABasicTestScript}),
                new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;
            ExpectKeysMatching(dict, new[] { "\\code.js" });
        }

        [Fact]
        public void Will_put_original_script_name_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ACoffeeTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            Assert.True(HasKeyWithSubstring(dict, "code.coffee"));
        }

        [Fact]
        public void Will_put_converted_source_code_in_coverage_object_for_coffee_script()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ACoffeeTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var codeCoffeeEntry = dict.Single(e => e.Key.Contains("code.coffee"));
            Assert.True(codeCoffeeEntry.Value.SourceLines.Any(l => l.Contains("function")));
        }

        [Fact]
        public void Will_put_source_code_in_coverage_object_for_js_script()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var codeCoffeeEntry = dict.Single(e => e.Key.Contains("code.js"));
            Assert.NotNull(codeCoffeeEntry.Value.SourceLines);
        }

        [Fact]
        public void Will_put_original_script_name_in_file_path_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ACoffeeTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var filePath = dict.Single(kvp => kvp.Key.Contains("code.coffee")).Value.FilePath;
            Assert.Contains("\\code.coffee", filePath);
        }

        [Fact]
        public void Will_not_put_file_uris_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            Assert.False(HasKeyWithSubstring(dict, "file://"));
        }

        [Theory]
        [PropertyData("RequireJsTestScripts")]
        public void Will_create_coverage_object_for_test_where_test_file_uses_requirejs_command(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObject);
        }

        [Theory]
        [PropertyData("RequireJsTestScripts")]
        public void Will_cover_the_correct_files_for_test_where_test_file_uses_requirejs_command(string scriptPath)
        {
            var testRunner = TestRunner.Create();
            testRunner.EnableDebugMode();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject,
                               new[]
                                   {
                                       scriptPath, 
                                       "tests\\base\\base.", 
                                       "tests\\ui\\ui.", 
                                       "\\base\\core.js",
                                       "ui\\screen.js",
                                       "jquery.js",
                                       "base\\jquery.alpha.js"
                                   });
        }

        [Theory]
        [PropertyData("RequireJsTestScripts")]
        public void Will_include_only_given_file_patterns(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => co.IncludePatterns = new[] { "*\\ui\\*", "*core.js" }), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject,
                               new[]
                                   {
                                      "tests\\ui\\ui.", "\\base\\core.js", "ui\\screen.js"
                                   });
        }

        [Theory]
        [PropertyData("RequireJsTestScripts")]
        public void Will_exclude_given_file_patterns(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => co.ExcludePatterns = new[] { "*\\ui\\*", "*core.js" }), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.CoverageObject,
                               new[]
                                   {
                                       scriptPath, "tests\\base\\base."
                                   });
        }

        [Theory]
        [PropertyData("RequireJsTestScripts")]
        public void Will_resolve_requirejs_required_files_correctly(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            var dict = result.TestFileSummaries.Single().CoverageObject;
            Assert.True(dict.Any(e => e.Key.Contains("\\RequireJS\\")));
        }

        [Theory]
        [PropertyData("RequireJsTestScripts")]
        public void Will_get_results_for_test_where_test_file_uses_requirejs_command_with_coverage_enabled(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => co.ExcludePatterns = new[]{"*\\require.js"}), new ExceptionThrowingRunnerCallback());

            Assert.Equal(2, result.TotalCount);
        }

        private TestOptions WithCoverage(params Action<CoverageOptions>[] mods)
        {
            var opts = new TestOptions
                           {
                               CoverageOptions = new CoverageOptions
                                                     {
                                                         Enabled = true
                                                     }
                           };
            mods.ToList().ForEach(a => a(opts.CoverageOptions));
            return opts;
        }

        private bool HasKeyWithSubstring<T>(IDictionary<string, T> dict, string subString)
        {
            return dict.Keys.Any(k => k.Contains(subString));
        }

        private void ExpectKeysMatching<T>(IDictionary<string, T> dict, IEnumerable<string> keySubstrings)
        {
            var ok = true;
            var keySubstringsList = keySubstrings.ToList();
            if (dict.Count != keySubstringsList.Count)
            {
                ok = false;
            }
            if (ok)
            {
                foreach (var substr in keySubstringsList)
                {
                    var found = dict.Keys.Any(key => key.IndexOf(substr, StringComparison.CurrentCultureIgnoreCase) >= 0);
                    if (!found)
                    {
                        ok = false;
                    }
                }
            }
            if (!ok)
            {
                throw new Xunit.Sdk.EqualException(string.Join(", ", keySubstringsList), string.Join(", ", dict.Keys), true);
            }
        }
    }
}
