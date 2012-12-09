using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Chutzpah.FileGenerator;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;

namespace Chutzpah.FileGenerators
{
    public abstract class CompileToJavascriptFileGenerator : IFileGenerator
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly ICompilerEngineWrapper compilerEngineWrapper;
        private readonly ICompilerCache compilerCache;

        protected CompileToJavascriptFileGenerator(IFileSystemWrapper fileSystem, ICompilerEngineWrapper compilerEngineWrapper, ICompilerCache compilerCache)
        {
            this.fileSystem = fileSystem;
            this.compilerEngineWrapper = compilerEngineWrapper;
            this.compilerCache = compilerCache;
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
            string jsText = null;

            if (GlobalOptions.Instance.EnableCompilerCache)
            {
                lock (compilerCache)
                {
                    jsText = compilerCache.Get(sourceText);
                }
            }
            if (string.IsNullOrEmpty(jsText))
            {
                jsText = compilerEngineWrapper.Compile(sourceText);
                if (GlobalOptions.Instance.EnableCompilerCache)
                {
                    lock (compilerCache)
                    {
                        compilerCache.Set(sourceText, jsText);
                    }
                }
            }
            var folderPath = Path.GetDirectoryName(referencedFile.Path);
            var fileName = Path.GetFileNameWithoutExtension(referencedFile.Path) + ".js";
            var newFilePath = Path.Combine(folderPath, string.Format(Constants.ChutzpahTemporaryFileFormat, Thread.CurrentThread.ManagedThreadId, fileName));
            
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