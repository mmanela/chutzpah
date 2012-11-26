using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Coverage;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    /// <summary>
    /// Note: Requires JSCover-all.jar in Facts.Integration\bin\Debug!
    /// </summary>
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

            var result = testRunner.RunTests(scriptPath, WithCoverage());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObject);
        }

        [Fact]
        public void Will_create_a_coverage_object_json_representation()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage());

            Assert.NotNull(result.TestFileSummaries.Single().CoverageObjectJson);
            Assert.Contains("code.js", result.TestFileSummaries.Single().CoverageObjectJson);
        }


        [Fact]
        public void Will_store_branch_data_in_the_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var branchData = dict.Single(kvp => kvp.Key.Contains("code.js")).Value.BranchData;
            Assert.NotNull(branchData);
        }

        [Fact]
        public void Will_store_branch_conditions_in_branch_data_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var branchData = dict.Single(kvp => kvp.Key.Contains("code.js")).Value.BranchData;
            Assert.Equal("i < a.length", branchData[4][1].Src); // line 4, condition 1
        }

        [Fact]
        public void Will_have_1_based_coverage_lines()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var coverageLines = dict.Single(kvp => kvp.Key.Contains("code.js")).Value.Coverage;
            Assert.Null(coverageLines[0]);
        }

        [Fact]
        public void Will_have_0_based_source_lines()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var sourceLines = dict.Single(kvp => kvp.Key.Contains("code.js")).Value.Source;
            Assert.Equal("var stringLib = {", sourceLines[0]);
        }

        [Fact]
        public void Will_not_create_a_coverage_object_if_coverage_is_disabled()
        {
            var testRunner = TestRunner.Create(true);

            var result = testRunner.RunTests(ABasicTestScript);

            Assert.Null(result.TestFileSummaries.Single().CoverageObject);
        }

        [Fact]
        public void Will_only_include_specified_files_in_coverage_report()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.IncludePattern = "**\\code.js"));
            var dict = result.TestFileSummaries.Single().CoverageObject;
            Assert.True(HasKeyWithSubstring(dict, "code.js"));
            Assert.False(HasKeyWithSubstring(dict, ABasicTestScript));
        }

        [Fact]
        public void Will_exclude_specified_files_from_the_coverage_report()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ABasicTestScript, WithCoverage(co => co.ExcludePattern = "**\\" + ABasicTestScript));
            var dict = result.TestFileSummaries.Single().CoverageObject;
            Assert.True(HasKeyWithSubstring(dict, "code.js"));
            Assert.False(HasKeyWithSubstring(dict, ABasicTestScript));
        }

        [Fact]
        public void Will_instrument_compiled_script_rather_than_original_script()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ACoffeeTestScript, WithCoverage());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            var sourceLines = dict.Single(kvp => kvp.Key.Contains("code.coffee")).Value.Source;
            Assert.True(sourceLines.Any(l => l.Contains("function()")));
        }

        [Fact]
        public void Will_put_original_script_name_in_coverage_object()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(ACoffeeTestScript, WithCoverage());
            var dict = result.TestFileSummaries.Single().CoverageObject;

            Assert.True(HasKeyWithSubstring(dict, "code.coffee"));
        }

        private TestOptions WithCoverage(params Action<CoverageOptions>[] mods)
        {
            var cov = CoverageEngineFactory.GetCoverageEngine();
            var messages = new List<string>();
            if (!cov.CanUse(messages))
            {
                throw new Exception(string.Join(" ", messages));
            }
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
    }
}
