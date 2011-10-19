namespace Chutzpah
{
    using System.Text.RegularExpressions;

    public static class RegexPatterns
    {
        public static Regex QUnitTestAndModuleRegex = new Regex(@"(\bmodule[\t ]*\([\t ]*[""'](?<Module>.*)[""'])|(\btest[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex QUnitTestRegex = new Regex(@"((?<!\.)\btest[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex JasmineTestRegex = new Regex(@"((?<!\.)\bdescribe[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);
    }
}
