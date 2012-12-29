using System;

namespace Chutzpah.Compilers
{
    public interface IJavaScriptCompiler : IDisposable
    {
        string Compile(string source, params object[] args);
    }
}