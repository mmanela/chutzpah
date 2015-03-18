using System.Collections.Generic;

namespace Chutzpah.Facts.Integration
{
    public static class TestPathGroups
    {
        public static IEnumerable<object[]> ChutzpahSamples
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
                    
                    new object[] {@"Samples\Compilation\ExternalCompile\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\TypeScript\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\CoffeeScript\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\TypeScriptMsbuild\chutzpah.json",2},
                    new object[] {@"Samples\Compilation\TypeScriptPowershell\chutzpah.json",2},


                    // Batching Tests
                    new object[] {@"Samples\Settings\Batching\chutzpah.json",3},

                    // Chutzpah Settings Inheritance Samples
                    new object[] {@"Samples\Settings\Inheritance\ParentInheritance\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\ParentInheritance\Multiplication\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\ParentInheritance\Multiplication\Power\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\PathInheritance\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\PathInheritance\Multiplication\chutzpah.json",2},
                    new object[] {@"Samples\Settings\Inheritance\PathInheritance\Power\chutzpah.json",2},

                };
            }
        }
    }
}