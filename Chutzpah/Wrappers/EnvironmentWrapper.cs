using System;
using System.Reflection;

namespace Chutzpah.Wrappers
{
    public class EnvironmentWrapper : IEnvironmentWrapper
    {
        public string[] GetCommandLineArgs()
        {
            return Environment.GetCommandLineArgs();
        }

        public string GetExeuctingAssemblyPath()
        {
            return Assembly.GetExecutingAssembly().Location;
        }
    }
}