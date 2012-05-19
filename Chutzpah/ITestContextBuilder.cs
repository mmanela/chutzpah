namespace Chutzpah
{
    using Chutzpah.Models;

    public interface ITestContextBuilder
    {
        TestContext BuildContext(string file);

        bool TryBuildContext(string file, out TestContext context);
        bool IsTestFile(string file);
    }
}