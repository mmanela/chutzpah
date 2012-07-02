using System.Diagnostics;
using System.IO;

namespace Chutzpah.Wrappers
{
    public interface IProcessWrapper
    {
        ProcessStartInfo StartInfo { get; set; }
        StreamReader StandardOutput { get; }
        StreamWriter StandardInput { get; }
        StreamReader StandardError { get; }
        bool Start();
        void WaitForExit();
        bool WaitForExit(int milliseconds);
        void Kill();
    }
}