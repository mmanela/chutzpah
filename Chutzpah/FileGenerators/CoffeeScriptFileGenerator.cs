using System;
using System.Linq;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class CoffeeScriptFileGenerator : CompileToJavascriptFileGenerator
    {
        private readonly ICoffeeScriptEngineWrapper coffeeScriptEngine;
        private readonly ICompilerCache compilerCache;

        public CoffeeScriptFileGenerator(IFileSystemWrapper fileSystem, ICoffeeScriptEngineWrapper coffeeScriptEngine, ICompilerCache compilerCache)
            : base(fileSystem)
        {
            this.coffeeScriptEngine = coffeeScriptEngine;
            this.compilerCache = compilerCache;
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.CoffeeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        public override IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles)
        {
            var compiledMap = (from referencedFile in referencedFiles
                               let content = fileSystem.GetText(referencedFile.Path)
                               let jsText = GetOrAddCompiledToCache(content)
                               select new { FileName = referencedFile.Path, Content = jsText })
                              .ToDictionary(x => x.FileName, x => x.Content);

            return compiledMap;
        }

        private string GetOrAddCompiledToCache(string content)
        {
            var cached = compilerCache.Get(content);
            if(string.IsNullOrEmpty(cached))
            {
                cached = coffeeScriptEngine.Compile(content);
                compilerCache.Set(content, cached);
            }

            return cached;
        }
    }
}