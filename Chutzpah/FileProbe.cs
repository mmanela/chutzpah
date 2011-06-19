using System.IO;
using Chutzpah.Wrappers;

namespace Chutzpah
{
    public class FileProbe : IFileProbe
    {
        private readonly IEnvironmentWrapper environment;
        private readonly IFileSystemWrapper fileSystem;

        public FileProbe(IEnvironmentWrapper environment, IFileSystemWrapper fileSystem)
        {
            this.environment = environment;
            this.fileSystem = fileSystem;
        }

        public FileProbe()
            : this(new EnvironmentWrapper(), new FileSystemWrapper())
        {
        }


        public string FindPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var executingPath = environment.GetExeuctingAssemblyPath();
            var executingDir = fileSystem.GetDirectoryName(executingPath);
            var filePath = Path.Combine(executingDir, fileName);
            if (fileSystem.FileExists(filePath))
                return filePath;

            var currentDirFilePath = fileSystem.GetFullPath(fileName);
            if (fileSystem.FileExists(currentDirFilePath))
                return currentDirFilePath;
  
            return null;
        }
    }
}