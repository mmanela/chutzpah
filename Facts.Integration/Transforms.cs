using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Chutzpah.Facts.Integration
{
    public class Transforms : CoverageBase
    {
        public Transforms()
        {
            ChutzpahTracer.Enabled = TestUtils.TracingEnabled;
        }

        [Fact]
        public void Will_generate_coverage_HTML()
        {
            var expectedHtmlPath = @"JS\Test\TestSettings\CodeCoverage\myCoverage.html";

            if (File.Exists(expectedHtmlPath))
            {
                File.Delete(expectedHtmlPath);
                Assert.False(File.Exists(expectedHtmlPath), "Didn't manage to delete left-over HTML file from " + expectedHtmlPath);
            }

            var scriptPath = @"JS\Test\TestSettings\CodeCoverage\cc.js";
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.True(File.Exists(expectedHtmlPath), "HTML output wasn't generated");
        }

        [Fact]
        public void Will_generate_coverage_JSON()
        {
            var expectedHtmlPath = @"JS\Test\TestSettings\CodeCoverage\myCoverage.json";

            if (File.Exists(expectedHtmlPath))
            {
                File.Delete(expectedHtmlPath);
                Assert.False(File.Exists(expectedHtmlPath), "Didn't manage to delete left-over JSON file from " + expectedHtmlPath);
            }

            var scriptPath = @"JS\Test\TestSettings\CodeCoverage\cc.js";
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.True(File.Exists(expectedHtmlPath), "JSON output wasn't generated");
        }

        [Fact]
        public void Will_generate_LCOV_output_when_configured_and_coverage_enabled()
        {
            var expectedLcovPath = @"JS\Test\TestSettings\Lcov\lcov.dat";

            if (File.Exists(expectedLcovPath))
            {
                File.Delete(expectedLcovPath);
                Assert.False(File.Exists(expectedLcovPath), "Didn't manage to delete left-over LCOV file from " + expectedLcovPath);
            }

            var scriptPath = @"JS\Test\TestSettings\Lcov\lcov.js";
            var libPath = @"JS\code\CoverageTarget.js";
            var testRunner = TestRunner.Create();

            var result = testRunner.RunTests(scriptPath, WithCoverage(), new ExceptionThrowingRunnerCallback());

            Assert.True(File.Exists(expectedLcovPath), "LCOV output wasn't generated");

            var expectedHeaderRegex = new Regex(string.Format("SF:.+{0}", libPath.Replace("\\", "\\\\")));
            var expectedBodyLines =
@"DA:1,1
DA:2,1
DA:4,1
DA:5,1
DA:6,0
DA:9,1
end_of_record".Split('\n').Select(x => x.Trim()).ToArray();

            var lcov = File.ReadAllLines(expectedLcovPath);

            Assert.True(expectedHeaderRegex.IsMatch(lcov[0]));
            for (var i = 0; i < expectedBodyLines.Length; i++){
                Assert.Equal(expectedBodyLines[i], lcov[i + 1]);
            }
        }
    }
}
