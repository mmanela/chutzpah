using SassAndCoffee.JavaScript.CoffeeScript;

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

        public string Compile(string coffeScriptSource)
        {
            return engine.Compile(coffeScriptSource, typeof(CoffeeScriptCompiler));
        }
    }
}