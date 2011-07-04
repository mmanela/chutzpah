namespace Chutzpah
{
    public interface IHtmlTestFileCreator
    {
        string CreateTestFile(string jsFile);
        string UpdateTestFolder(string jsFile, string generatedHtmlFilePath);
    }
}