using Chutzpah.Models;
namespace Chutzpah.Wrappers
{
    public interface ICompilerEngineWrapper
    {
        string Compile(string source, params object[] args);
    }
}