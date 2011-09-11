using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Chutzpah
{
    public static class RegexPatterns
    {
        public static Regex QUnitTestAndModuleRegex = new Regex(@"(\bmodule[\t ]*\([\t ]*[""'](?<Module>.*)[""'])|(\btest[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);
        public static Regex QUnitTestRegex = new Regex(@"(\btest[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);
    }
}
