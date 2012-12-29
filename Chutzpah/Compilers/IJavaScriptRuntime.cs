using System;

namespace Chutzpah.Compilers
{
    public interface IJavaScriptRuntime : IDisposable
    {
        void Initialize();
        void LoadLibrary(string libraryCode);
        T ExecuteFunction<T>(string functionName, params object[] args);
        dynamic AsDynamic();
    }
}