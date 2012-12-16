using System;

namespace Chutzpah.Compilers.CoffeeScript
{
    public class CoffeeScriptCompiler : JavaScriptCompilerBase
    {
        public override string CompilerLibraryResourceName
        {
            get { return "coffee-script.js"; }
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