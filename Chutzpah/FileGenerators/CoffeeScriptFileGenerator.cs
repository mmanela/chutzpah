using System;
using System.Linq;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class CoffeeScriptFileGenerator : CompileToJavascriptFileGenerator
    {
        private readonly ICoffeeScriptEngineWrapper coffeeScriptEngine;

        public CoffeeScriptFileGenerator(IFileSystemWrapper fileSystem, ICoffeeScriptEngineWrapper coffeeScriptEngine)
            : base(fileSystem)
        {
            this.coffeeScriptEngine = coffeeScriptEngine;
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        protected override IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles)
        {
            var compiledMap = (from referencedFile in referencedFiles
                               let content = fileSystem.GetText(referencedFile.Path)
                               let jsText = coffeeScriptEngine.Compile(content)
                               select new { FileName = referencedFile.Path, Content = jsText })
                              .ToDictionary(x => x.FileName, x => x.Content);

            return compiledMap;
        }
    }
}