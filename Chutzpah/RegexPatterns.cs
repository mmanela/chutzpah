namespace Chutzpah
{
    using System.Text.RegularExpressions;

    public static class RegexPatterns
    {
        public static Regex QUnitTestRegexJavaScript = new Regex(@"((?<!\.)\b(?:QUnit\.)?(?<Tf>test|asyncTest)[\t ]*\([\t ]*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);
        public static Regex QUnitTestRegexCoffeeScript = new Regex(@"(^[\t ]*(?:QUnit\.)?(?<Tf>test|asyncTest)[\t ]+[""'](?<Test>.*)[""'])", RegexOptions.Compiled | RegexOptions.Multiline);

        public static Regex JasmineTestAndModuleRegex = new Regex(@"(\bdescribe\s*\(\s*[""'](?<Module>.*)[""'])|(\bit\s*\(\s*[""'](?<Test>.*)[""'])", RegexOptions.Compiled);

        public static Regex JasmineTestRegexJavaScript = new Regex(@"(?<!\.)\b(?<Tf>it)\s*\(\s*[""'](?<Test>.*)[""']", RegexOptions.Compiled);
        public static Regex JasmineTestRegexCoffeeScript = new Regex(@"^[\t ]*(?<Tf>it)[\t ]+[""'](?<Test>.*)[""']", RegexOptions.Compiled | RegexOptions.Multiline);

        public static Regex SchemePrefixRegex = new Regex(@"^(http|https|file)://", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex InvalidPrefixedLocalFilePath = new Regex(@"^\/([a-z]:/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
