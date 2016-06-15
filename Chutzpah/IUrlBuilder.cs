using Chutzpah.Models;

namespace Chutzpah
{
    public interface IUrlBuilder
    {
        string GenerateFileUrl(TestContext testContext, ReferencedFile referencedFile);
        string GenerateFileUrl(TestContext testContext, string absolutePath, bool fullyQualified = false, bool isBuiltInDependency = false);
    }
}