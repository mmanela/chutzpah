using System;

namespace Chutzpah.Compilers.TypeScript
{
    public enum TypeScriptCodeGenTarget
    {
        ES3,
        ES5
    }

    public enum TypeScriptModuleKind
    {
        CommonJS,
        AMD
    }

    public class TypeScriptCompiler : JavaScriptCompilerBase
    {
        public override string[] CompilerLibraryResourceNames
        {
            get { return new [] {"polyfill.js", "typescript.js", "json2.js", "compile-ts.js"}; }
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