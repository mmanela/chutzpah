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

        public static Regex IsJasmineFileName = new Regex("^jasmine(-[0-9]+\\.[0-9]+\\.[0-9]+)?\\.js$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex IsQUnitFileName = new Regex("^qunit(-[0-9]+\\.[0-9]+\\.[0-9]+)?\\.js$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex IsRequireJsFileName = new Regex("^require(-[0-9]+\\.[0-9]+\\.[0-9]+)?\\.js$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        public static readonly Regex MochaBddTestRegexCoffeeScript = JasmineTestRegexCoffeeScript;
        public static readonly Regex MochaBddTestRegexJavaScript = JasmineTestRegexJavaScript;
        public static readonly Regex MochaTddTestRegexCoffeeScript = QUnitTestRegexCoffeeScript;
        public static readonly Regex MochaTddTestRegexJavaScript = QUnitTestRegexJavaScript;
        public static readonly Regex MochaExportsTestRegexCoffeeScript = new Regex(@"^\s*(?<Tf>[""'](?<Test>.*)[""']\s*:\s*\([^)]*\)\s*->)", RegexOptions.Compiled | RegexOptions.Multiline);
        public static readonly Regex MochaExportsTestRegexJavaScript = new Regex(@"^\s*(?<Tf>[""'](?<Test>.*)[""']\s*:\s*function\()", RegexOptions.Compiled | RegexOptions.Multiline);
    }
}
