using System;
using System.Linq;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public class TypeScriptFileGenerator : CompileToJavascriptFileGenerator
    {
        private readonly ITypeScriptEngineWrapper typeScriptEngine;
        private readonly IJsonSerializer jsonSerializer;

        public TypeScriptFileGenerator(IFileSystemWrapper fileSystem, ITypeScriptEngineWrapper typeScriptEngine, IJsonSerializer jsonSerializer)
            : base(fileSystem)
        {
            this.typeScriptEngine = typeScriptEngine;
            this.jsonSerializer = jsonSerializer;
        }

        public override bool CanHandleFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(Constants.TypeScriptExtension, StringComparison.OrdinalIgnoreCase);
        }

        public override IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles, ChutzpahTestSettingsFile chutzpahTestSettings)
        {
            var referenceList = (from referencedFile in referencedFiles
                                 let content = fileSystem.GetText(referencedFile.Path)
                                 select new { FileName = referencedFile.Path, Content = content }).ToList();

            var compiledMap = new Dictionary<string, string>();
            var needsCompileMap = referenceList.ToDictionary(x => x.FileName, x => x.Content);
            if (needsCompileMap.Count > 0)
            {
                var needsCompileMapJson = jsonSerializer.Serialize(needsCompileMap);

                var resultJson = typeScriptEngine.Compile(needsCompileMapJson, chutzpahTestSettings.TypeScriptCodeGenTarget.ToString());
                compiledMap = jsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
            }

            return compiledMap;
        }
    }
}