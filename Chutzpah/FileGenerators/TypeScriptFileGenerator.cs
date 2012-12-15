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

        protected override IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles)
        {
            var sourceMap = (from referencedFile in referencedFiles
                             let content = fileSystem.GetText(referencedFile.Path)
                             select new { FileName = referencedFile.Path, Content = content })
                            .ToDictionary(x => x.FileName, x => x.Content);
            var sourceMapJson = jsonSerializer.Serialize(sourceMap);

            var resultJson = typeScriptEngine.Compile(sourceMapJson);
            var compiledMap = jsonSerializer.Deserialize<IDictionary<string, string>>(resultJson);
            return compiledMap;
        }
    }
}