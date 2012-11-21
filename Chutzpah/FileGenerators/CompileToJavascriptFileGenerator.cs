using System;
using System.Collections.Generic;
using System.IO;
using Chutzpah.FileGenerator;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public abstract class CompileToJavascriptFileGenerator : IFileGenerator
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly ICompilerEngineWrapper compilerEngineWrapper;

        protected CompileToJavascriptFileGenerator(IFileSystemWrapper fileSystem, ICompilerEngineWrapper compilerEngineWrapper)
        {
            this.fileSystem = fileSystem;
            this.compilerEngineWrapper = compilerEngineWrapper;
        }

        /// <summary>
        /// This will get called for the test file and all referenced files. 
        /// If the referenced file can be handled it generate a .js file and sets to the reference files generatedfilepath and adds the new file path to the temporary file collection
        /// If it can't handle the file it does nothing
        /// </summary>
        /// <param name="referencedFile"></param>
        /// <param name="temporaryFiles"></param>
        public virtual void Generate(ReferencedFile referencedFile, IList<string> temporaryFiles)
        {
            if (!CanHandleFile(referencedFile)) return;

            var sourceText = fileSystem.GetText(referencedFile.Path);
            var jsText = compilerEngineWrapper.Compile(sourceText);
            var folderPath = Path.GetDirectoryName(referencedFile.Path);
            var fileName = Path.GetFileNameWithoutExtension(referencedFile.Path) + ".js";
            var newFilePath = Path.Combine(folderPath, string.Format(Constants.ChutzpahTemporaryFileFormat, fileName));
            
            fileSystem.WriteAllText(newFilePath, jsText);
            referencedFile.GeneratedFilePath = newFilePath;
            temporaryFiles.Add(newFilePath);
        }

        /// <summary>
        /// Determines if this generator can handle the referencefile.
        /// This must be overridden in the base class
        /// </summary>
        /// <param name="referencedFile"></param>
        /// <returns></returns>
        public abstract bool CanHandleFile(ReferencedFile referencedFile);
    }
}