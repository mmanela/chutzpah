using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chutzpah.Server
{
    public class ChutzpahServerFileProvider : IFileProvider
    {
        PhysicalFileProvider fileProvider;
        string builtInDependencyFolder;

        public ChutzpahServerFileProvider(string root, string builtInDependencyFolder)
        {
            fileProvider = new PhysicalFileProvider(root);
            this.builtInDependencyFolder = builtInDependencyFolder;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return fileProvider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            // This implements of simple virtual directly fo chutzpah internal files
            if (subpath.StartsWith("/" + Constants.ServerVirtualBuiltInFilesPath, StringComparison.OrdinalIgnoreCase))
            {
                var path = subpath.Replace("/" + Constants.ServerVirtualBuiltInFilesPath, UrlBuilder.NormalizeUrlPath(builtInDependencyFolder));

                var fileInfo = new FileInfo(path);
                if(!fileInfo.FullName.StartsWith(builtInDependencyFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // Prevent any shenanigans of someone trying to access file they shouldn't
                    return new NotFoundFileInfo(subpath);
                }

                return new PhysicalFileInfo(fileInfo);
            }
            else
            {
                return fileProvider.GetFileInfo(subpath);
            }
        }

        public IChangeToken Watch(string filter)
        {
            return fileProvider.Watch(filter);
        }
    }
}
