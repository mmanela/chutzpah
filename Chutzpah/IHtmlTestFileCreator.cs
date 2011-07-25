using Chutzpah.Models;
namespace Chutzpah
{
    public interface ITestContextBuilder
    {
        TestContext BuildContext(string jsFile);
        TestContext BuildContext(string jsFile, string generatedHtmlFilePath);
    }
}