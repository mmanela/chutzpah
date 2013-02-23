using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestContextBuilder
    {
        TestContext BuildContext(PathInfo file, TestOptions options);
        TestContext BuildContext(string file, TestOptions options);
        bool TryBuildContext(PathInfo file, TestOptions options, out TestContext context);
        bool TryBuildContext(string file, TestOptions options, out TestContext context);
        bool IsTestFile(string file);
        void CleanupContext(TestContext context);
    }
}