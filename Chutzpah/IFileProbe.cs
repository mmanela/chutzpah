using System.Collections.Generic;
using Chutzpah.Models;
namespace Chutzpah
{
    public interface IFileProbe
    {
        /// <summary>
        /// Finds the full path of a file. This method will probe from both the executing assembly
        /// path. As well as the current working directory.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>The full path to the file</returns>
        string FindFilePath(string fileName);

        /// <summary>
        /// Finds the full path of a folder. This method will probe from both the executing assembly
        /// path. As well as the current working directory.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>The full path to the folder</returns>
        string FindFolderPath(string folderName);


        /// <summary>
        /// Given a list of paths will return items which *may* be testable files.
        /// If a folder is in the list will search the folder for additional files that 
        /// may be testable
        /// </summary>
        /// <param name="testPaths">The test paths.</param>
        /// <param name="testingMode">The testing mode.</param>
        /// <returns>A list of files to test</returns>
        IEnumerable<string> FindScriptFiles(IEnumerable<string> testPaths, TestingMode testingMode);

        /// <summary>
        /// Gets the type and full path of the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The type and full path the path points to.</returns>
        PathInfo GetPathInfo(string path);
    }
}