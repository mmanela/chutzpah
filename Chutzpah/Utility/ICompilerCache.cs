using System;

namespace Chutzpah.Utility
{
    public interface ICompilerCache
    {
        string Get(string key);
        void Set(string key, string compiled);
        void Save();
    }
}