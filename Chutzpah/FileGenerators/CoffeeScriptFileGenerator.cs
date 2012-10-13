using System;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class CoffeeScriptFileGenerator : CompileToJavascriptFileGenerator
    {

        public CoffeeScriptFileGenerator(IFileSystemWrapper fileSystem, ICoffeeScriptEngineWrapper coffeeScriptEngine)
            :base(fileSystem,coffeeScriptEngine)
        {
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}