using Chutzpah.Compilers.TypeScript;

namespace Chutzpah.Wrappers
{
    public interface ITypeScriptEngineWrapper : ICompilerEngineWrapper
    {
    }

    public class TypeScriptEngineWrapper : ITypeScriptEngineWrapper
    {
        private readonly SingleThreadedJavaScriptHostedCompiler engine;

        public TypeScriptEngineWrapper()
        {
            engine = new SingleThreadedJavaScriptHostedCompiler();
        }

        public string Compile(string typeScriptSource, params object[] args)
        {
            return engine.Compile(typeScriptSource, typeof(TypeScriptCompiler), args);
        }
    }
}