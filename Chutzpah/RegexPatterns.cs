namespace Chutzpah
{
    using System.Text.RegularExpressions;

    public static class RegexPatterns
    {
        public static Regex QUnitTestAndModuleRegex = new Regex(@"(\bmodule[\t ]*\([\t ]*[""'](?<Module>.*)[""'])|(\btest[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex QUnitTestRegex = new Regex(@"((?<!\.)\btest[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex JasmineTestAndModuleRegex = new Regex(@"(\bdescribe\s*\(\s*[""'](?<Module>.*)[""'])|(\bit\s*\(\s*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex JasmineTestRegex = new Regex(@"(?<!\.)\bit\s*\(\s*[""'](?<Test>.*)[""']", RegexOptions.Compiled);
    }
}
