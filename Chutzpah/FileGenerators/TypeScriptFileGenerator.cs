using System;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class TypeScriptFileGenerator : CompileToJavascriptFileGenerator
    {

        public TypeScriptFileGenerator(IFileSystemWrapper fileSystem, ITypeScriptEngineWrapper typeScriptEngine, ICompilerCache compilerCache)
            : base(fileSystem, typeScriptEngine,compilerCache)
        {
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.TypeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}