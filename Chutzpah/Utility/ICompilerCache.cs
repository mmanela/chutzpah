using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Utility
{
    public interface ICompilerCache
    {
        string Get(string source);
        void Set(string source, string compiled);
    }
}
