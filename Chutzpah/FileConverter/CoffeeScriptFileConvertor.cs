using System;
using System.Collections.Generic;
using System.IO;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileConverter
{
    public interface ICoffeeScriptFileConverter
    {
        void Convert(ReferencedFile referencedFile);
    }

    public class CoffeeScriptFileConverter : ICoffeeScriptFileConverter
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly ICoffeeScriptEngineWrapper coffeeScriptEngine;

        public CoffeeScriptFileConverter(IFileSystemWrapper fileSystem, ICoffeeScriptEngineWrapper coffeeScriptEngine)
        {
            this.fileSystem = fileSystem;
            this.coffeeScriptEngine = coffeeScriptEngine;
        }

        public void Convert(ReferencedFile referencedFile)
        {
            if (!IsCoffeeScriptFile(referencedFile)) return;

            var coffeeText = fileSystem.GetText(referencedFile.Path);
            var jsText = coffeeScriptEngine.Compile(coffeeText);
            var folderPath = Path.GetDirectoryName(referencedFile.Path);
            var fileName = Path.GetFileNameWithoutExtension(referencedFile.Path) + ".js";
            var newFilePath = Path.Combine(folderPath, string.Format(Constants.ChutzpahTemporaryFileFormat, fileName));
            fileSystem.WriteAllText(newFilePath, jsText);
            referencedFile.Path = newFilePath;
        }

        private static bool IsCoffeeScriptFile(ReferencedFile referencedFile)
        {
            return referencedFile.Path.EndsWith(".coffee", StringComparison.OrdinalIgnoreCase);
        }
    }
}