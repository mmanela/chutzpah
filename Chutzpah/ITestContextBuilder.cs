using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestContextBuilder
    {
        TestContext BuildContext(PathInfo file, TestOptions options);
        TestContext BuildContext(string file, TestOptions options);
        bool TryBuildContext(PathInfo file, TestOptions options, out TestContext context);
        bool TryBuildContext(string file, TestOptions options, out TestContext context);


        TestContext BuildContext(IEnumerable<PathInfo> files, TestOptions options);
        TestContext BuildContext(IEnumerable<string> files, TestOptions options);
        bool TryBuildContext(IEnumerable<PathInfo> files, TestOptions options, out TestContext context);
        bool TryBuildContext(IEnumerable<string> files, TestOptions options, out TestContext context);

        bool IsTestFile(string file, ChutzpahSettingsFileEnvironments environments = null);
        void CleanupContext(TestContext context);
    }
}