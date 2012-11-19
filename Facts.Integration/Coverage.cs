using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
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

            var result = testRunner.RunTests(scriptPath, WithCoverage(AndInclude("**\\code.js")));
            var text = result.TestFileSummaries.Single().CoverageObject.ToString();
            Assert.Contains("code.js", text);
            Assert.DoesNotContain(scriptPath.Replace("\\", "\\\\"), text);
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
        public void Will_exclude_specified_files_from_the_coverage_report(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(AndExclude("**\\" + scriptPath)));
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

        private Action<TestOptions> AndInclude(string includePattern)
        {
            return opts =>
                       {
                           opts.CoverageOptions.IncludePattern = includePattern;
                       };
        }

        private Action<TestOptions> AndExclude(string excludePattern)
        {
            return opts =>
            {
                opts.CoverageOptions.ExcludePattern = excludePattern;
            };
        }

        private TestOptions WithCoverage(params Action<TestOptions>[] modifiers)
        {
            var opts = new TestOptions
                           {
                               CoverageOptions = new CoverageOptions
                                                     {
                                                         Enabled = true
                                                     }
                           };
            modifiers.ToList().ForEach(mod => mod(opts));
            return opts;
        }
    }
}
