namespace Chutzpah
{
    using System.Text.RegularExpressions;

    public static class RegexPatterns
    {
        public static Regex QUnitTestRegexJavaScript = new Regex(@"((?<!\.)\b(?:QUnit\.)?(test|asyncTest)[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);
        public static Regex QUnitTestRegexCoffeeScript = new Regex(@"(^[\t ]*(?:QUnit\.)?(test|asyncTest)[\t ]+[""'](?<Test>.*)[""'])", RegexOptions.Compiled | RegexOptions.Multiline);

        public static Regex JasmineTestAndModuleRegex = new Regex(@"(\bdescribe\s*\(\s*[""'](?<Module>.*)[""'])|(\bit\s*\(\s*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex JasmineTestRegexJavaScript = new Regex(@"(?<!\.)\bit\s*\(\s*[""'](?<Test>.*)[""']", RegexOptions.Compiled);
        public static Regex JasmineTestRegexCoffeeScript = new Regex(@"^[\t ]*it[\t ]+[""'](?<Test>.*)[""']", RegexOptions.Compiled | RegexOptions.Multiline);

        public static Regex SchemePrefixRegex = new Regex(@"^(http|https|file)://", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
