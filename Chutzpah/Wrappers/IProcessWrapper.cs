namespace Chutzpah.Wrappers
{
    public interface IProcessWrapper
    {
        string RunExecutableAndCaptureOutput(string exePath, string arguments);
    }
}