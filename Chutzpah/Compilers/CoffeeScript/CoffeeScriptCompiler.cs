using System;

namespace Chutzpah.Compilers.CoffeeScript
{
    public class CoffeeScriptCompiler : JavaScriptCompilerBase
    {
        public override string[] CompilerLibraryResourceNames
        {
            get { return new[] {"coffee-script.js", "compile-cs.js"}; }
        }

        public override string CompilationFunctionName
        {
            get { return "compilify_cs"; }
        }

        public CoffeeScriptCompiler(Lazy<IJavaScriptRuntime> jsRuntimeProvider)
            : base(jsRuntimeProvider)
        {
        }
    }
}