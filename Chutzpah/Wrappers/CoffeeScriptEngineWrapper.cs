using System.IO;
using Chutzpah.Utility;
using SassAndCoffee.JavaScript.CoffeeScript;

namespace Chutzpah.Wrappers
{
    public interface ICoffeeScriptEngineWrapper : ICompilerEngineWrapper
    {
    }

    public class CoffeeScriptEngineWrapper : ICoffeeScriptEngineWrapper
    {
        private readonly SingleThreadedJavaScriptHostedCompiler _engine;
        private static readonly CompilerCache CoffeeCache = new CompilerCache(Path.Combine(Path.GetTempPath(),"_Chutzpah_compilercache"));

        public CoffeeScriptEngineWrapper()
        {
            _engine = new SingleThreadedJavaScriptHostedCompiler();
        }

        ~CoffeeScriptEngineWrapper()
        {
            CoffeeCache.Save();
        }

       

        

        public string Compile(string coffeScriptSource)
        {
            string compiled = "";
            lock (CoffeeCache)
            {
                compiled = CoffeeCache.Get(coffeScriptSource);
            }
            
            if (string.IsNullOrEmpty(compiled))
            {
                compiled = _engine.Compile(coffeScriptSource, typeof (CoffeeScriptCompiler));
                CoffeeCache.Set(coffeScriptSource, compiled);
            }
            return compiled;
        }
    }
}