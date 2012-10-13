using SassAndCoffee.JavaScript.TypeScript;

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

        public string Compile(string coffeScriptSource)
        {
            return engine.Compile(coffeScriptSource, typeof(TypeScriptCompiler));
        }
    }
}