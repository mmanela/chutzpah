using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Wrappers;
using Chutzpah.Models;

namespace Chutzpah.TestFileDetectors
{
    public class QUnitTestableFileDetector : ITestableFileDetector
    {
        private readonly IFileSystemWrapper fileSystem;
        
        public QUnitTestableFileDetector(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public bool IsTestableFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException("filePath");

            var text = fileSystem.GetText(filePath);
            return RegexPatterns.QUnitTestRegex.IsMatch(text);
        }
    }
}
