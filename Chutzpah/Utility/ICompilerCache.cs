using System;

namespace Chutzpah.Utility
{
    public interface ICompilerCache
    {
        string Get(string source);
        void Set(string source, string compiled);
        void Save();
    }
}