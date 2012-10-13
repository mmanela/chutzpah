using System;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class TypeScriptFileGenerator : CompileToJavascriptFileGenerator
    {

        public TypeScriptFileGenerator(IFileSystemWrapper fileSystem, ITypeScriptEngineWrapper typeScriptEngine)
            : base(fileSystem, typeScriptEngine)
        {
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.TypeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}