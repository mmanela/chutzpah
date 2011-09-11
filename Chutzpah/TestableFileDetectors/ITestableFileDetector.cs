using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.TestFileDetectors
{
    public interface ITestableFileDetector
    {
        bool IsTestableFile(string filePath);
    }
}
