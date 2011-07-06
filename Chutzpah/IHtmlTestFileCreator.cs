namespace Chutzpah
{
    public interface IHtmlTestFileCreator
    {
        string CreateTestFile(string jsFile);
        string CreateTestFile(string jsFile, string generatedHtmlFilePath);
    }
}