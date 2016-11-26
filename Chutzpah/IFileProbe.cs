using System.Collections.Generic;
using Chutzpah.Models;
namespace Chutzpah
{
    public interface IFileProbe
    {
        /// <summary>
        /// The directory where built in dependencies can be found
        /// </summary>
        string BuiltInDependencyDirectory { get; }
        
        /// <summary>
        /// Finds the full path of a file. This method will probe from both the executing assembly
        /// path. As well as the current working directory.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>The full path to the file</returns>
        string FindFilePath(string path);

        /// <summary>
        /// Finds the full path of a folder. This method will probe from both the executing assembly
        /// path. As well as the current working directory.
        /// </summary>
        /// <param name="path">Folder path.</param>
        /// <returns>The full path to the folder</returns>
        string FindFolderPath(string path);


        /// <summary>
        /// Given a list of paths will return items which *may* be testable files.
        /// If a folder is in the list will search the folder for additional files that 
        /// may be testable
        /// </summary>
        /// <param name="testPaths">The test paths.</param>
        /// <param name="testingMode">The testing mode.</param>
        /// <returns>A list of files to test</returns>
        IEnumerable<PathInfo> FindScriptFiles(IEnumerable<string> testPaths);

        /// <summary>
        /// Gets the type and full path of the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The type and full path the path points to.</returns>
        PathInfo GetPathInfo(string path);

        IEnumerable<PathInfo> FindScriptFiles(string path);

        /// <summary>
        /// Determines if the file is a temporary chutzpah file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsTemporaryChutzpahFile(string path);

        /// <summary>
        /// Finds a Chutzpah test settings file given a directory. Will recursively scan current direcotry 
        /// and all directories above until it finds the file 
        /// </summary>
        /// <param name="currentDirectory">the directory to start searching from</param>
        /// <returns>Eithe the found setting file path or null</returns>
        string FindTestSettingsFile(string currentDirectory);



        /// <summary>
        /// Given a chutzpah test settings file find the test files it specifies
        /// </summary>
        /// <param name="chutzpahTestSettings">The chutzpah test settings file.</param>
        /// <returns>A list of files to test</returns>
        IEnumerable<PathInfo> FindScriptFiles(ChutzpahTestSettingsFile chutzpahTestSettings);

        /// <summary>
        /// Is this a chutzpah settings file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool IsChutzpahSettingsFile(string path);


        /// <summary>
        /// Gets the content of a reference file and updates its hash
        /// </summary>
        string GetReferencedFileContent(ReferencedFile file, ChutzpahTestSettingsFile settings);

        void SetReferencedFileHash(ReferencedFile file, ChutzpahTestSettingsFile settings);
    }
}