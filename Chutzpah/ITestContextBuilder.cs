namespace Chutzpah
{
    using Chutzpah.Models;

    public interface ITestContextBuilder
    {
        TestContext BuildContext(string file);

        TestContext BuildContext(string file, string generatedHtmlFilePath);

        bool TryBuildContext(string file, out TestContext context);

        bool TryBuildContext(string file, string generatedHtmlFilePath, out TestContext context);
    }
}