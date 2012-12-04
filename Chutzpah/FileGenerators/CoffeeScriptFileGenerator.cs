using System;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class CoffeeScriptFileGenerator : CompileToJavascriptFileGenerator
    {

        public CoffeeScriptFileGenerator(IFileSystemWrapper fileSystem, ICoffeeScriptEngineWrapper coffeeScriptEngine, ICompilerCache compilerCache)
            :base(fileSystem,coffeeScriptEngine,compilerCache)
        {
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}