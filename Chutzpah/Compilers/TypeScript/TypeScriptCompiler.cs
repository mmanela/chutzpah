using System;

namespace Chutzpah.Compilers.TypeScript
{
    public class TypeScriptCompiler : JavaScriptCompilerBase
    {
        public override string CompilerLibraryResourceName
        {
            get { return "typescript.js"; }
        }

        public override string CompilationFunctionName
        {
            get { return "compilify_ts"; }
        }

        public TypeScriptCompiler(Lazy<IJavaScriptRuntime> jsRuntimeProvider)
            : base(jsRuntimeProvider)
        {
        }
    }
}