using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    public class Coverage : CoverageBase
    {
        public Coverage()
        {
            ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
        }

        private const string ABasicTestScript = @"JS\Test\basic-jasmine.js";
        private const string ASourceMappedTestScript = @"JS\Test\Coverage\SourceMaps\Tests.ts";

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
                    new object[] {@"JS\Test\basic-mocha-qunit.js", null},
                };
            }
        }

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

        public static IEnumerable<object[]> AmdTestScriptWithAMDMode_BASE
        {
            get
            {
                return new[]
                    {
                       new object[] {@"JS\Code\AMDMode_RequireJS\tests\base\base.qunit.test.js"},
                    };
            }
        }

        public static IEnumerable<object[]> AmdTestScriptWithAMDMode_UI
        {
            get
            {
                return new[]
                    {
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\ui\ui.qunit.test.js"},
                    };
            }
        }

        public static IEnumerable<object[]> ManualStartAmdTests
        {
            get { return TestPathGroups.ManualStartAmdTests; }
        }


        public static IEnumerable<object[]> ChutzpahSamples
        {
            get { return TestPathGroups.ChutzpahSamplesWithCoverageSupported; }
        }

        [Theory]
        [MemberData("ChutzpahSamples")]
        public void Will_run_coverage_from_chutzpah_amd_samples(string scriptPath, int count)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.Equal(count, result.TotalCount);
        }

        [Theory]
        [MemberData("ManualStartAmdTests")]
        public void Will_run_manual_start_amd_tests(string scriptPath, int count)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.Equal(count, result.TotalCount);
        }

        [Fact]
        public void Will_exclude_files_from_settings_file_using_requirejs()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\ExcludePathWithRequireJS\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\Coverage\ExcludePathWithRequireJS\test.js" });
        }

        [Theory]
        [MemberData("BasicTestScripts")]
        public void Will_create_a_coverage_object(string scriptPath, string frameworkVersion)
        {
            var testRunner = TestRunner.Create();
            testRunner.EnableDebugMode();
            ChutzpahTestSettingsFile.Default.FrameworkVersion = frameworkVersion;

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObject);
        }

        [Theory]
        [MemberData("BasicTestScripts")]
        public void Will_cover_the_correct_scripts(string scriptPath, string frameworkVersion)
        {
            var testRunner = TestRunner.Create();
            ChutzpahTestSettingsFile.Default.FrameworkVersion = frameworkVersion;

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { scriptPath, "JS\\Code\\code.js" });
        }

        [Theory]
        [MemberData("BasicTestScripts")]
        public void Will_get_test_results_with_coverage_enabled(string scriptPath, string frameworkVersion)
        {
            var testRunner = TestRunner.Create();
            ChutzpahTestSettingsFile.Default.FrameworkVersion = frameworkVersion;

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void Will_turn_on_code_coverage_given_setting_in_json_file()
        {
            var scriptPath = @"JS\Test\TestSettings\CodeCoverage\cc.js";
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { scriptPath, "JS\\Code\\code.js" });
        }

        [Fact]
        public void Will_get_coverage_for_path_with_hash_tag()
        {
            if (ChutzpahTestSettingsFile.ForceWebServerMode) { return; }

            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\PathEncoding\C#\pathEncoding.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\PathEncoding\C#\pathEncoding.js", "JS\\Code\\code.js" });
        }

        [Fact]
        public void Will_get_coverage_for_path_with_space()
        {
            if (ChutzpahTestSettingsFile.ForceWebServerMode) { return; }

            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\PathEncoding\With Space+\pathEncoding.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { @"JS\Test\PathEncoding\With Space+\pathEncoding.js", "JS\\Code\\code.js" });
        }

        [Fact]
        public void Will_include_files_from_settings_file()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\IncludePath\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject, new[] { "JS\\Code\\code.js" });
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
        public void Will_create_null_coverage_object_if_coverage_is_disabled()
        {
            var testRunner = TestRunner.Create(true);

            var result = testRunner.RunTests(ABasicTestScript, new ExceptionThrowingRunnerCallback());

            Assert.Null(result.TestFileSummaries.Single().CoverageObject);
        }

        [Fact]
        public void Will_only_include_specified_files_in_coverage_report()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.IncludePatterns = new[] { "**\\code.js" }),
                new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;
            ExpectKeysMatching(dict, new[] { "\\code.js" });
        }

        [Fact]
        public void Will_exclude_specified_files_from_the_coverage_report()
        {
            var testRunner = TestRunner.Create();
            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.ExcludePatterns = new[] { "**\\" + ABasicTestScript }),
                new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;
            ExpectKeysMatching(dict, new[] { "\\code.js" });
        }

        [Fact]
        public void Will_put_original_script_name_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ASourceMappedTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            Assert.True(HasKeyWithSubstring(dict, "SourceMappedLibrary.ts"));
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
        public void Will_put_orginal_source_code_in_coverage_object_for_TypeScript_with_source_maps_enabled()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ASourceMappedTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var tsEntry = dict.Single(e => e.Key.Contains("SourceMappedLibrary.ts"));
            Assert.True(tsEntry.Value.SourceLines.Any(l => l.Contains("module SourceMaps.Library")));
        }

        [Fact]
        public void Will_convert_covered_lines_via_source_maps_when_enabled()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ASourceMappedTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var tsEntry = dict.Single(e => e.Key.Contains("SourceMappedLibrary.ts"));
            Assert.Equal(28, tsEntry.Value.LineExecutionCounts.Length);
            Assert.Equal(0.875, tsEntry.Value.CoveragePercentage, 3);
        }

        [Fact]
        public void Will_put_original_script_name_in_file_path_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ASourceMappedTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var filePath = dict.Single(kvp => kvp.Key.Contains("SourceMappedLibrary.ts")).Value.FilePath;
            Assert.Contains("\\SourceMappedLibrary.ts", filePath);
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
        [MemberData("AmdTestScriptWithForcedRequire")]
        [MemberData("AmdTestScriptWithAMDMode")]
        public void Will_create_coverage_object_for_test_where_test_file_uses_requirejs_command(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObject);
            Assert.True(result.TestFileSummaries.Single().CoverageObject.Count > 0);
        }

        [Theory]
        [MemberData("AmdTestScriptWithAMDMode_BASE")]
        public void Will_cover_where_test_file_uses_amd_mode_with_base_scripts(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject,
                               new[]
                                   {
                                       scriptPath, "\\base\\core.js",
                                   });
        }

        [Theory]
        [MemberData("AmdTestScriptWithAMDMode_UI")]
        public void Will_cover_where_test_file_uses_amd_mode_with_UI_scripts(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject,
                               new[]
                                   {
                                       scriptPath, "\\base\\core.js", "ui\\screen.js"
                                   });
        }

        [Theory]
        [MemberData("AmdTestScriptWithForcedRequire")]
        public void Will_cover_the_correct_files_for_test_where_test_file_uses_requirejs_command(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject,
                               new[]
                                   {
                                       scriptPath, "tests\\base\\base.", "tests\\ui\\ui.", "\\base\\core.js",
                                       "ui\\screen.js"
                                   });
        }

        [Theory]
        [MemberData("AmdTestScriptWithForcedRequire")]
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
        [MemberData("AmdTestScriptWithForcedRequire")]
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
        [MemberData("AmdTestScriptWithForcedRequire")]
        public void Will_ignore_only_given_file_patterns(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => { co.IncludePatterns = new[] { "*\\ui\\*", "*core.js" }; co.IgnorePatterns = new[] { "*\\tests\\*" }; }), new ExceptionThrowingRunnerCallback());

            ExpectKeysMatching(result.TestFileSummaries.Single().CoverageObject,
                               new[]
                                   {
                                      "\\base\\core.js", "ui\\screen.js"
                                   });
        }

        [Theory]
        [MemberData("AmdTestScriptWithForcedRequire")]
        public void Will_resolve_requirejs_required_files_correctly(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            var dict = result.TestFileSummaries.Single().CoverageObject;
            Assert.True(dict.Any(e => e.Key.Contains("\\RequireJS\\")));
        }

        [Theory]
        [MemberData("AmdTestScriptWithForcedRequire")]
        public void Will_get_results_for_test_where_test_file_uses_requirejs_command_with_coverage_enabled(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => co.ExcludePatterns = new[] { "*\\require.js" }), new ExceptionThrowingRunnerCallback());

            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public void Will_get_custom_success_percentage()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(@"JS\Test\Coverage\SuccessPercentage\test.js", WithCoverage(), new ExceptionThrowingRunnerCallback());

            var coverage = result.CoverageObject;
            Assert.Equal(94.34, coverage.SuccessPercentage);
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
                throw new Xunit.Sdk.EqualException(string.Join(", ", keySubstringsList), string.Join(", ", dict.Keys));
            }
        }
    }
}
