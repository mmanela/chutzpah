using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Coverage;
using Chutzpah.Models;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    /// <summary>
    /// Note: Requires JSCover-all.jar in Facts.Integration\bin\Debug!
    /// </summary>
    public class Coverage
    {
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

        public static IEnumerable<object[]> CoffeeScriptTests
        {
            get
            {
                return new[]
                {
                    new object[] { @"JS\Test\basic-jasmine.coffee" }
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

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_not_create_a_coverage_object_if_coverage_is_disabled(string scriptPath)
        {
            var testRunner = TestRunner.Create(true);

            var result = testRunner.RunTests(scriptPath);

            Assert.Null(result.TestFileSummaries.Single().CoverageObject);
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_fix_coverage_object_serialization(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage());
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();
            Assert.Contains("\"source\"", text);
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_emit_proper_escaped_paths_in_coverage_report(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage());
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();
            Assert.Contains(scriptPath.Replace("\\", "\\\\"), text);
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_only_include_specified_files_in_coverage_report(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => co.IncludePattern = "**\\code.js"));
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();
            Assert.Contains("code.js", text);
            Assert.DoesNotContain(scriptPath.Replace("\\", "\\\\"), text);
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_exclude_specified_files_from_the_coverage_report(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(co => co.ExcludePattern = "**\\" + scriptPath));
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();
            Assert.Contains("code.js", text);
            Assert.DoesNotContain(scriptPath.Replace("\\", "\\\\"), text);
        }

        [Theory]
        [PropertyData("CoffeeScriptTests")]
        public void Will_instrument_compiled_script(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage());
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();

            Assert.Contains("function()", text);
        }

        [Theory]
        [PropertyData("CoffeeScriptTests")]
        public void Will_put_original_script_name_in_coverage_object(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage());
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();

            Assert.Contains("code.coffee", text);
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
    }
}
