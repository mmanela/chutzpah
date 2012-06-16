using System;
using System.IO;
using Chutzpah.Models;

namespace Chutzpah.Wrappers
{
    public interface IProcessHelper
    {
        void LaunchFileInBrowser(string file);
        ProcessResult<T> RunExecutableAndProcessOutput<T>(string exePath, string arguments, Func<StreamReader,T> streamProcessor);
    }
}