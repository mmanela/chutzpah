using System;
using System.IO;

namespace Chutzpah.Compilers
{
    public abstract class JavaScriptCompilerBase : IJavaScriptCompiler
    {
        public abstract string[] CompilerLibraryResourceNames { get; }
        public abstract string CompilationFunctionName { get; }

        private readonly Lazy<IJavaScriptRuntime> jsRuntimeProvider;
        private IJavaScriptRuntime js;
        private bool initialized;
        private readonly object syncLock = new object();

        public JavaScriptCompilerBase(Lazy<IJavaScriptRuntime> jsRuntimeProvider)
        {
            this.jsRuntimeProvider = jsRuntimeProvider;
        }

        public string Compile(string source, params object[] args)
        {
            if (source == null)
                throw new ArgumentException("source cannot be null.", "source");

            object[] compileArgs = null;
            if (args != null && args.Length > 0)
            {
                compileArgs = new object[args.Length + 1];
                compileArgs[0] = source;
                args.CopyTo(compileArgs, 1);
            }
            else
            {
                compileArgs = new object[] {source};
            }

            lock (syncLock)
            {
                Initialize();
                return js.ExecuteFunction<string>(CompilationFunctionName, compileArgs);
            }
        }

        private void Initialize()
        {
            if (!initialized)
            {
                js = jsRuntimeProvider.Value;
                js.Initialize();
                foreach (var resource in CompilerLibraryResourceNames)
                {
                    js.LoadLibrary(ReadEmbeddedResource(resource, GetType()));
                }
                initialized = true;
            }
        }

        private string ReadEmbeddedResource(string resource, Type scope)
        {
            using (var resourceStream = scope.Assembly.GetManifestResourceStream(scope, resource))
            using (var reader = new StreamReader(resourceStream))
            {
                return reader.ReadToEnd();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (js != null)
                {
                    js.Dispose();
                    js = null;
                }
            }
        }
    }
}