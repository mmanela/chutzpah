using Chutzpah.Models;

namespace Chutzpah
{
    public interface IUrlBuilder
    {
        string GenerateLocalFileUrl(string absolutePath);
        string GenerateFileUrl(TestContext testContext, ReferencedFile referencedFile);
        string GenerateFileUrl(TestContext testContext, string absolutePath, bool fullyQualified = false, bool isBuiltInDependency = false, string fileHash = null);

        string GenerateAbsoluteServerUrl(TestContext testContext, ReferencedFile referencedFile);

        string GenerateServerFileUrl(TestContext testContext, string absolutePath, bool fullyQualified, bool isBuiltInDependency, string fileHash);
    }
}