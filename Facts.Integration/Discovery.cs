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


        public static IEnumerable<object[]> CoffeeScriptTests
        {
            get
            {
                return new[]
                {
                    new object[] { @"JS\Test\basic-qunit-coffee.coffee" },
                    new object[] { @"JS\Test\basic-jasmine-coffee.coffee" },
                    new object[] { @"JS\Test\basic-mocha-bdd-coffee.coffee" }
                };
            }
        }


        public static IEnumerable<object[]> TypeScriptTests
        {
            get
            {
                return new[]
                        {
                            new object[] {@"JS\Test\TypeScript\basic-qunit.ts"},
                            new object[] {@"JS\Test\TypeScript\basic-jasmine.ts"},
                            new object[] {@"JS\Test\TypeScript\basic-mocha-bdd.ts"},
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
                    new object[] {@"JS\Code\RequireJS\all.tests.jasmine.js"},
                    new object[] {@"JS\Code\RequireJS\all.tests.mocha-qunit.js"},
                    new object[] {@"JS\Code\RequireJS\MochaWithSettings\all.tests.mocha-qunit.js"},
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
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\ui\ui.qunit.test.js"},
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\base\base.jasmine.test.js"},
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\ui\ui.jasmine.test.js"},
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\base\base.mocha-qunit.test.js"},
                        new object[] {@"JS\Code\AMDMode_RequireJS\tests\ui\ui.mocha-qunit.test.js"},
                    };
            }
        }


        public static IEnumerable<object[]> AMDTypeScriptTestScripts
        {
            get
            {
                return new[]
                {
                        new object[] {@"JS\Code\TypeScriptRequireJS\tests\base\base.qunit.test.ts"},
                        new object[] {@"JS\Code\TypeScriptRequireJS\tests\ui\ui.qunit.test.ts"},
                        new object[] {@"JS\Code\TypeScriptRequireJS\tests\base\base.jasmine.test.ts"},
                        new object[] {@"JS\Code\TypeScriptRequireJS\tests\ui\ui.jasmine.test.ts"},
                        new object[] {@"JS\Code\TypeScriptRequireJS\tests\base\base.mocha-qunit.test.ts"},
                        new object[] {@"JS\Code\TypeScriptRequireJS\tests\ui\ui.mocha-qunit.test.ts"},
                };
            }
        }

        public static IEnumerable<object[]> ChutzpahSamples
        {
            get { return TestPathGroups.ChutzpahSamples; }
        }


        public Discovery()
        {
            ChutzpahTracer.Enabled = false;
        }


        [Theory]
        [PropertyData("ChutzpahSamples")]
        public void Will_discover_amd_tests_from_chutzpah_samples(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(2, result.Count());
        }

        [Theory]
        [PropertyData("BasicTestScripts")]
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
        [PropertyData("CoffeeScriptTests")]
        public void Will_discover_tests_from_a_coffee_script_file(string scriptPath)
        {
            var testRunner = TestRunner.Create();
            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(4, result.Count());
            Assert.Equal("A basic test", result.ElementAt(0).TestName);
            Assert.Equal("will multiply 5 to number", result.ElementAt(3).TestName);
            Assert.Equal("mathLib", result.ElementAt(3).ModuleName);
        }


        [Theory]
        [PropertyData("AmdTestScriptWithForcedRequire")]
        public void Will_discover_amd_forced_require_tests(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(2, result.Count());
        }

        [Theory]
        [PropertyData("AmdTestScriptWithAMDMode")]
        [PropertyData("AMDTypeScriptTestScripts")]
        public void Will_discover_amd_mode_tests(string scriptPath)
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(scriptPath);

            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void Will_get_qunit_tests_from_a_folder()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\SubFolder");

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Will_get_tests_from_a_folder_and_a_file()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(new List<string> { @"JS\Test\basic-qunit.js", @"JS\Test\SubFolder" });

            Assert.Equal(6, result.Count());
        }

        [Fact]
        public void Will_get_async_test()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(new List<string> { @"JS\Test\asyncTest.js" });

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_without_module()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\basic-qunit.js");

            var test = result.SingleOrDefault(x => x.TestName.Equals("A basic test"));
            Assert.Equal(3, test.Line);
            Assert.Equal(2, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_qunit_test_with_module()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\basic-qunit.js");

            var test = result.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(11, test.Line);
            Assert.Equal(3, test.Column);
        }

        [Fact]
        public void Will_get_file_position_for_jasmine_test()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\basic-jasmine.js");

            var test = result.SingleOrDefault(x => x.TestName.Equals("will get vowel count"));
            Assert.Equal(13, test.Line);
            Assert.Equal(5, test.Column);
        }

        [Fact]
        public void Will_get_qunit_tests_from_a_html_file()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\basic-qunit.html");

            Assert.Equal(4, result.Count());
        }

        [Fact]
        public void Will_find_tests_with_characters_that_need_encoding()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\encoding.js");

            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void Will_find_tests_that_has_a_reference_to_web_url()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\webReference.js");

            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void Will_discover_tests_in_multiple_files_and_aggregate_results()
        {
            var testRunner = TestRunner.Create();
            var tests = new List<string>
                            {
                                @"JS\Test\basic-qunit.js",
                                @"JS\Test\basic-qunit.html"
                            };
            var result = testRunner.DiscoverTests(tests);

            Assert.Equal(8, result.Count());
        }

        [Fact]
        public void Will_find_test_in_file_which_has_script_error_which_gets_logged_to_output()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\scriptError.js");

            Assert.Equal(4, result.Count());
        }


        [Fact]
        public void Will_get_correct_module_name_for_nested_jasmine_suites()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\nestedJasmine.js");

            Assert.Equal("nested.jasmine hello", result.First().ModuleName);
        }

        [Fact]
        public void Will_get_qunit_tests_from_a_html_file_when_using_old_version_of_qunit()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\oldCallbackQUnit.html");

            Assert.Equal(4, result.Count());
            Assert.Equal("A basic test", result.ElementAt(0).TestName);
            Assert.Equal("will multiply 5 to number", result.ElementAt(3).TestName);
            Assert.Equal("mathLib", result.ElementAt(3).ModuleName);
        }

        [Fact]
        public void Will_get_test_from_qunit_html_file_with_inline_tests()
        {
            var testRunner = TestRunner.Create();

            var result = testRunner.DiscoverTests(@"JS\Test\inlineTests.html");

            Assert.Equal(1, result.Count());
            Assert.Equal("A basic test", result.ElementAt(0).TestName);
        }
    }
}