using Chutzpah.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.FileProcessors
{
    public class SourceMapDiscoverer : ISourceMapDiscoverer
    {
        IFileSystemWrapper fileSystemWrapper;

        public SourceMapDiscoverer(IFileSystemWrapper fileSystemWrapper)
        {
            if (fileSystemWrapper == null)
            {
                throw new ArgumentNullException("fileSystemWrapper");
            }

            this.fileSystemWrapper = fileSystemWrapper;
        }

        public string FindSourceMap(string sourceFilePath)
        {
            if (sourceFilePath == null)
            {
                throw new ArgumentNullException("sourceFilePath");
            }

            // Start with the filename convention of having .map suffixed
            var mapPath = sourceFilePath + ".map";
            if (fileSystemWrapper.FileExists(mapPath))
            {
                return mapPath;
            }

            return null;
        }
    }
}
