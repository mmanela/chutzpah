using System;
using System.Runtime.InteropServices;

namespace Chutzpah
{
    public class NativeImports
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        public static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadLibrary(string dllToLoad);
    }
}