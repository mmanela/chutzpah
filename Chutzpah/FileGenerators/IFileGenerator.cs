using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah.FileGenerator
{
    /// <summary>
    /// Interface which gets run during the test context building. It provider an opportunity to generate a new file for a test file and its dependencies.
    /// A good use for this is to support other programming languages that compile to JavaScript (for example CoffeeScript). 
    /// </summary>
    public interface IFileGenerator
    {
        /// <summary>
        /// This will get called for the test file and all referenced files. 
        /// </summary>
        /// <param name="referencedFile"></param>
        /// <param name="temporaryFiles"></param>
        void Generate(ReferencedFile referencedFile, IList<string> temporaryFiles);
    }
}