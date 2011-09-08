namespace Chutzpah.Wrappers
{
    public interface IProcessHelper
    {
        string RunExecutableAndCaptureOutput(string exePath, string arguments);
        void LaunchFileInBrowser(string file);
    }
}