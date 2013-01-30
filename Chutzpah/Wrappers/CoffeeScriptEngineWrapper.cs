using Chutzpah.Compilers.CoffeeScript;

namespace Chutzpah.Wrappers
{
    public interface ICoffeeScriptEngineWrapper : ICompilerEngineWrapper
    {
    }

    public class CoffeeScriptEngineWrapper : ICoffeeScriptEngineWrapper
    {
        private readonly SingleThreadedJavaScriptHostedCompiler engine;

        public CoffeeScriptEngineWrapper()
        {
            engine = new SingleThreadedJavaScriptHostedCompiler();
        }

        public string Compile(string coffeScriptSource, params object[] args)
        {
            return engine.Compile(coffeScriptSource, typeof(CoffeeScriptCompiler), args);
        }
    }
}