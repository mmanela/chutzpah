using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Integration
{
    public class Discovery
    {
        public static IEnumerable<object[]> BasicTestScripts
        {
            get
            {
                return new object[][]
                {
                        new object[] { @"JS\Test\basic-qunit.js", null },
                        new object[] { @"JS\Test\basic-jasmine.js", "1" },
                        new object[] { @"JS\Test\basic-jasmine.js", "2" },
                        new object[] {@"JS\Test\basic-mocha-bdd.js", null},
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


        public static IEnumerable<object[]> ChutzpahSamples
        {
            get { return TestPathGroups.ChutzpahSamples; }
        }

        public static IEnumerable<object[]> SkippedTests
        {
            get { return TestPathGroups.SkippedTests; }
        }


        public Discovery()
        {
            ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
        }


        [Theory]
        [MemberData("ChutzpahSamples")]
        public void Will_discover_amd_tests_from_chutzpah_samples(string scriptPath, int count)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(count, result.Count());
        }

        [Theory]
        [MemberData("BasicTestScripts")]
        public void Will_discover_tests_from_a_js_file(string scriptPath, string frameworkVersion)
        {
            var testRunner = TestRunner.Create();
            ChutzpahTestSettingsFile.Default.FrameworkVersion = frameworkVersion;

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(4, result.Count());
            Assert.Equal("A basic test", result.ElementAt(0).TestName);
            Assert.Equal("will multiply 5 to number", result.ElementAt(3).TestName);
            Assert.Equal("mathLib", result.ElementAt(3).ModuleName);
        }


        [Theory]
        [MemberData("AmdTestScriptWithForcedRequire")]
        public void Will_discover_amd_forced_require_tests(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(2, result.Count());
        }

        [Theory]
        [MemberData("AmdTestScriptWithAMDMode")]
        public void Will_discover_amd_mode_tests(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(1, result.Count());
        }

        [Theory]
        [MemberData("SkippedTests")]
        public void Will_discover_skipped_tests(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(3, result.Count());
        }
        
    }
}