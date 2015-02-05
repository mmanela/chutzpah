using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;


namespace Chutzpah.FileProcessors
{
    /// <summary>
    /// Interface of a class which can process a referenced file
    /// </summary>
    public interface IReferencedFileProcessor
    {
        void Process(IFrameworkDefinition frameworkDefinition, ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings);
    }
}
