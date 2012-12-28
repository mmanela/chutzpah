using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    public class Coverage
    {
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

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_create_a_coverage_object(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObject);
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
        public void Will_not_create_a_coverage_object_if_coverage_is_disabled()
        {
            var testRunner = TestRunner.Create(true);

            var result = testRunner.RunTests(ABasicTestScript, new ExceptionThrowingRunnerCallback());

            Assert.Null(result.TestFileSummaries.Single().CoverageObject);
        }

        [Fact]
        public void Will_only_include_specified_files_in_coverage_report()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.IncludePattern = "**\\code.js"),
                new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;
            Assert.True(HasKeyWithSubstring(dict, "code.js"));
            Assert.False(HasKeyWithSubstring(dict, ABasicTestScript));
        }

        [Fact]
        public void Will_exclude_specified_files_from_the_coverage_report()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.ExcludePattern = "**\\" + ABasicTestScript),
                new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;
            Assert.True(HasKeyWithSubstring(dict, "code.js"));
            Assert.False(HasKeyWithSubstring(dict, ABasicTestScript));
        }

        [Fact]
        public void Will_put_original_script_name_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ACoffeeTestScript, WithCoverage(), new ExceptionThrowingRunnerCallback());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            Assert.True(HasKeyWithSubstring(dict, "code.coffee"));
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

        private class ExceptionThrowingRunnerCallback : RunnerCallback
        {
            public override void ExceptionThrown(Exception exception, string fileName)
            {
                throw exception;
            }
        }
    }
}
