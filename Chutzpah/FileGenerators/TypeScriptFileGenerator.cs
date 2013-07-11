using System;
using System.Linq;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Chutzpah.Compilers.TypeScript;

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
                                 select new TypeScriptFile { FileName = referencedFile.Path, Content = content }).ToList();


            InsertLibDeclarationFile(referenceList);

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

        /// <summary>
        /// Add the lib.d.ts declaration as the first file. We need to do this since we load typescript in memory so it can't find
        /// its own local lib.d.ts file
        /// </summary>
        /// <param name="referenceList"></param>
        private void InsertLibDeclarationFile(List<TypeScriptFile> referenceList)
        {
            var libDeclarationFileName = "lib.d.ts";
            
            // If the user already included their own lib.d.ts then don't add our own
            if (!referenceList.Any(x => fileSystem.GetFileName(x.FileName).Equals(libDeclarationFileName, StringComparison.OrdinalIgnoreCase)))
            {
                var libDeclarationFileContent = EmbeddedManifestResourceReader.GetEmbeddedResoureText<TypeScriptCompiler>(libDeclarationFileName);
                referenceList.Insert(0, new TypeScriptFile { FileName = libDeclarationFileName, Content = libDeclarationFileContent });
            }
        }

        private class TypeScriptFile
        {
            public string FileName { get; set; }
            public string Content { get; set; }
        }
    }
}