using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Chutzpah.FileGenerator;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public abstract class CompileToJavascriptFileGenerator : IFileGenerator
    {
        protected readonly IFileSystemWrapper fileSystem;

        protected CompileToJavascriptFileGenerator(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// This will get called for the test file and all referenced files. 
        /// If the referenced file can be handled it generate a .js file and sets to the reference files generatedfilepath and adds the new file path to the temporary file collection
        /// If it can't handle the file it does nothing
        /// </summary>
        public virtual void Generate(IEnumerable<ReferencedFile> referencedFiles, IList<string> temporaryFiles)
        {
            // Filter down to just the referenced files this generator supports
            referencedFiles = referencedFiles.Where(CanHandleFile).ToList();

            var compiledMap = GenerateCompiledSources(referencedFiles);

            foreach (var referencedFile in referencedFiles)
            {
                if (!compiledMap.ContainsKey(referencedFile.Path)) continue;

                var jsText = compiledMap[referencedFile.Path];
                WriteGeneratedReferencedFile(referencedFile, jsText, temporaryFiles);
            }
        }

        /// <summary>
        /// Determines if this generator can handle the referencefile.
        /// This must be overridden in the base class
        /// </summary>
        public abstract bool CanHandleFile(ReferencedFile referencedFile);


        protected void WriteGeneratedReferencedFile(ReferencedFile referencedFile, string generatedContent,
                                                    IList<string> temporaryFiles)
        {
            var folderPath = Path.GetDirectoryName(referencedFile.Path);
            var fileName = Path.GetFileNameWithoutExtension(referencedFile.Path) + ".js";
            var newFilePath = Path.Combine(folderPath, string.Format(Constants.ChutzpahTemporaryFileFormat,
                                                         Thread.CurrentThread.ManagedThreadId, fileName));

            fileSystem.WriteAllText(newFilePath, generatedContent);
            referencedFile.GeneratedFilePath = newFilePath;
            temporaryFiles.Add(newFilePath);
        }

        public abstract IDictionary<string, string> GenerateCompiledSources(IEnumerable<ReferencedFile> referencedFiles);
    }
}