using System.Collections.Generic;
using System.Linq;

namespace Chutzpah.Facts.Integration
{
    public static class TestPathGroups
    {
        public static IEnumerable<object[]> ManualStartAmdTests
        {
            get
            {
                return new[]
                    {
                        new object[] {@"JS\Test\ManualAmdStart\QUnit-ManualStart\chutzpah.json", 2},
                        new object[] {@"JS\Test\ManualAmdStart\Mocha-ManualStart\chutzpah.json", 2},
                        new object[] {@"JS\Test\ManualAmdStart\Jasmine-ManualStart\chutzpah.json", 2},
                    };
            }
        }

        public static IEnumerable<object[]> SkippedTests
        {
            get
            {
                return new[]
                    {
                        new object[] {@"JS\Test\Skipped\skippedQunit.js"},
                        new object[] {@"JS\Test\Skipped\skippedMocha.js"},
                        new object[] {@"JS\Test\Skipped\skippedJasmine.js"},
                    };
            }
        }

        public static IEnumerable<object[]> ChutzpahSamplesWithCoverageSupported
        {
            get
            {
                return new[]
                {
                    new object[] {@"Samples\RequireJS\QUnit\chutzpah.json",2},
                    new object[] {@"Samples\RequireJS\Mocha\chutzpah.json",2},
                    new object[] {@"Samples\RequireJS\Jasmine\chutzpah.json",2},
                    new object[] {@"Samples\RequireJS\TypeScript\chutzpah.json",2},

                    new object[] {@"Samples\RequireJS\CustomBaseUrl\QUnit\chutzpah.json",2},
                    new object[] {@"Samples\RequireJS\CustomBaseUrlAndCustomHarnessLocation\QUnit\chutzpah.json",2},
                    new object[] {@"Samples\RequireJS\CustomHarnessLocation\QUnit\chutzpah.json",2},
                    
                    // Compilation Tests
                    new object[] {@"Samples\Compilation\ExternalCompile\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\TypeScript\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\TypeScriptMsbuild\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\TypeScriptPowershell\chutzpah.json",2},


                    new object[] { @"Samples\Compilation\TypeScriptToSingleFile\chutzpah.json", 2},
                    new object[] { @"Samples\Compilation\TypeScriptToSingleFileExceptForTests\chutzpah.json", 2},


                    // Batching Tests
                    new object[] {@"Samples\Settings\Batching\chutzpah.json",3},

                    // Chutzpah Settings Inheritance Samples
                    new object[] {@"Samples\Settings\Inheritance\ParentInheritance\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\ParentInheritance\Multiplication\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\ParentInheritance\Multiplication\Power\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\PathInheritance\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\PathInheritance\Multiplication\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\PathInheritance\Power\chutzpah.json",2},


                    // Angular Samples
                    new object[] { @"Samples\Angular\Basic_TypeScript\tests\chutzpah.json", 1},
                    new object[] { @"Samples\Angular\TemplateDirective\chutzpah.json", 2},
  
                    // React Samples
                    
                    new object[] { @"Samples\React\Basic\chutzpah.json", 1},

                };
            }
        }


        public static IEnumerable<object[]> ChutzpahSamples
        {
            get
            {
                // Angular 2 Samples which do not support code coverage
                return ChutzpahSamplesWithCoverageSupported.Concat(new[] { new object[] { @"Samples\Angular2\Basic\chutzpah.json", 2 } });
            }
        }
    }
}