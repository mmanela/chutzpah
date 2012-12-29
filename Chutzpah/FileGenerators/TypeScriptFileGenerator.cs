using System;
using System.Linq;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class TypeScriptFileGenerator : CompileToJavascriptFileGenerator
    {
        private readonly ITypeScriptEngineWrapper typeScriptEngine;
        private readonly IJsonSerializer jsonSerializer;
        private readonly ICompilerCache compilerCache;

        public TypeScriptFileGenerator(IFileSystemWrapper fileSystem, ITypeScriptEngineWrapper typeScriptEngine, IJsonSerializer jsonSerializer, ICompilerCache compilerCache)
            : base(fileSystem)
        {
            this.typeScriptEngine = typeScriptEngine;
            this.jsonSerializer = jsonSerializer;
            this.compilerCache = compilerCache;
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.TypeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        public override IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles)
        {
            var referenceList = (from referencedFile in referencedFiles
                                 let content = fileSystem.GetText(referencedFile.Path)
                                 let cachedCompile = compilerCache.Get(content)
                                 select new { FileName = referencedFile.Path, Content = content, Compiled = cachedCompile }).ToList();

            var cachedCompileMap = referenceList.Where(x => x.Compiled != null).ToDictionary(x => x.FileName, x => x.Compiled);

            var compiledMap = new Dictionary<string, string>();
            var needsCompileMap = referenceList.Where(x => x.Compiled == null).ToDictionary(x => x.FileName, x => x.Content);
            if (needsCompileMap.Count > 0)
            {
                var needsCompileMapJson = jsonSerializer.Serialize(needsCompileMap);

                var resultJson = typeScriptEngine.Compile(needsCompileMapJson);
                compiledMap = jsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);

                // Set newly compiled items into cache
                foreach (var pair in compiledMap)
                {
                    compilerCache.Set(needsCompileMap[pair.Key], pair.Value);
                }
            }

            return compiledMap.Concat(cachedCompileMap).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}