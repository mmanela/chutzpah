namespace Chutzpah.FileProcessors
{
    using Chutzpah.Models;

    /// <summary>
    /// Interface of a class which can process a referenced file
    /// </summary>
    public interface IReferencedFileProcessor
    {
        void Process(ReferencedFile referencedFile);
    }
}
