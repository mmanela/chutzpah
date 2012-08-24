using Chutzpah.Models;

namespace Chutzpah
{
    public interface ITestContextBuilder
    {
        TestContext BuildContext(string file);

        bool TryBuildContext(string file, out TestContext context);
        bool IsTestFile(string file);
        void CleanupContext(TestContext context);
    }
}