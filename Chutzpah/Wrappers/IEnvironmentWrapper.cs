namespace Chutzpah.Wrappers
{
    public interface IEnvironmentWrapper
    {
        string[] GetCommandLineArgs();
        string GetExeuctingAssemblyPath();
    }
}