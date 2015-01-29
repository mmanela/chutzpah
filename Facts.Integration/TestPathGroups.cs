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
                    new object[] {@"Samples\RequireJS\QUnit\chutzpah.json"},
                    new object[] {@"Samples\RequireJS\Mocha\chutzpah.json"},
                    new object[] {@"Samples\RequireJS\Jasmine\chutzpah.json"},
                    new object[] {@"Samples\RequireJS\TypeScript\chutzpah.json"},

                    new object[] {@"Samples\RequireJS\CustomBaseUrl\QUnit\chutzpah.json"},
                    new object[] {@"Samples\RequireJS\CustomBaseUrlAndCustomHarnessLocation\QUnit\chutzpah.json"},
                    new object[] {@"Samples\RequireJS\CustomHarnessLocation\QUnit\chutzpah.json"},
                    
                    new object[] {@"Samples\Compilation\ExternalCompile\chutzpah.json"},
                    new object[] {@"Samples\Compilation\TypeScript\chutzpah.json"},
                    new object[] {@"Samples\Compilation\CoffeeScript\chutzpah.json"},
                    new object[] {@"Samples\Compilation\TypeScriptMsbuild\chutzpah.json"},
                    new object[] {@"Samples\Compilation\TypeScriptPowershell\chutzpah.json"},


                    // Chutzpah Settings Inheritance Samples
                    new object[] {@"Samples\SettingsInheritance\BasicScenario\chutzpah.json"},
                    new object[] {@"Samples\SettingsInheritance\BasicScenario\Multiplication\chutzpah.json"},
                    new object[] {@"Samples\SettingsInheritance\BasicScenario\Multiplication\Power\chutzpah.json"},

                };
            }
        }
    }
}