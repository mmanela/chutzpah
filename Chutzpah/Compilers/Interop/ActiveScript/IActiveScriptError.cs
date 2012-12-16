using System.Runtime.InteropServices;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// An object implementing this interface is passed to the IActiveScriptSite.OnScriptError method
    /// whenever the scripting engine encounters an unhandled error. The host then calls methods on
    /// this object to obtain information about the error that occurred.
    /// </summary>
    [Guid("EAE1BA61-A4ED-11cf-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActiveScriptError
    {
        /// <summary>
        /// Retrieves information about an error that occurred while the scripting engine was running
        /// a script.
        /// </summary>
        /// <param name="exceptionInfo">An EXCEPINFO structure that receives error information.</param>
        void GetExceptionInfo(out EXCEPINFO exceptionInfo);

        /// <summary>
        /// Retrieves the location in the source code where an error occurred while the scripting engine
        /// was running a script.
        /// </summary>
        /// <param name="sourceContext">A cookie that identifies the context. The interpretation of
        /// this parameter depends on the host application.</param>
        /// <param name="lineNumber">The line number in the source file where the error occurred.</param>
        /// <param name="characterPosition">The character position in the line where the error occurred.</param>
        void GetSourcePosition(out uint sourceContext, out uint lineNumber, out int characterPosition);

        /// <summary>
        /// Retrieves the line in the source file where an error occurred while a scripting engine
        /// was running a script.
        /// </summary>
        /// <param name="sourceLine">The line of source code in which the error occurred.</param>
        void GetSourceLineText([MarshalAs(UnmanagedType.BStr)] out string sourceLine);
    }
}