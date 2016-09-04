using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
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
            if(subpath.StartsWith("/" + Constants.ServerVirtualBuiltInFilesPath, StringComparison.OrdinalIgnoreCase))
            {
                subpath = subpath.Replace(Constants.ServerVirtualBuiltInFilesPath, UrlBuilder.NormalizeUrlPath(builtInDependencyFolder));
            }

            return fileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return fileProvider.Watch(filter);
        }
    }
}
