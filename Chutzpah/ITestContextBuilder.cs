using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestContextBuilder
    {
        TestContext BuildContext(PathInfo file);
        TestContext BuildContext(string file);
        bool TryBuildContext(PathInfo file, out TestContext context);
        bool TryBuildContext(string file, out TestContext context);
        bool IsTestFile(string file);
        void CleanupContext(TestContext context);
    }
}