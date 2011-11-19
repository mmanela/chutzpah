using Chutzpah.Models;

namespace Chutzpah.Wrappers
{
    public interface IProcessHelper
    {
        ProcessResult RunExecutableAndCaptureOutput(string exePath, string arguments);
        void LaunchFileInBrowser(string file);
    }
}