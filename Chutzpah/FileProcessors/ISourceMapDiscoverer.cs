using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.FileProcessors
{
    public interface ISourceMapDiscoverer
    {
        string FindSourceMap(string sourceFilePath);
    }
}
