using System;
using SassAndCoffee.Core;
using SassAndCoffee.JavaScript;
using SassAndCoffee.JavaScript.CoffeeScript;

namespace Chutzpah.Wrappers
{
    public interface ICoffeeScriptEngineWrapper
    {
        string Compile(string coffeScriptSource);
    }

    public class CoffeeScriptEngineWrapper : ICoffeeScriptEngineWrapper
    {
        private readonly Lazy<CoffeeScriptCompiler> engine;

        public CoffeeScriptEngineWrapper()
        {
            engine = new Lazy<CoffeeScriptCompiler>(CreateEngine);
        }

        public string Compile(string coffeScriptSource)
        {
            return engine.Value.Compile(coffeScriptSource);
        }

        private static CoffeeScriptCompiler CreateEngine()
        {
            var provider = new InstanceProvider<IJavaScriptRuntime>(() => new IEJavaScriptRuntime());
            var compiler = new CoffeeScriptCompiler(provider);
            return compiler;
        }
    }
}