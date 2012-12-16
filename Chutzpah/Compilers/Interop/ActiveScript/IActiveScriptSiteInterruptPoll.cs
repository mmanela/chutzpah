using System.Runtime.InteropServices;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// The IActiveScriptSiteInterruptPoll interface allows a host to specify that a script should terminate.
    /// </summary>
    [Guid("539698A0-CDCA-11CF-A5EB-00AA0047A063")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActiveScriptSiteInterruptPoll
    {
        /// <summary>
        /// Allows a host to specify that a script should terminate.
        /// </summary>
        /// <remarks>Throw a COMException with HRESULT S_FALSE to indicate the call succeeded and the host
        /// requests that the script terminate. Else you're signalling that the call succeeded and the host
        /// permits the script to continue running.</remarks>
        void QueryContinue();
    }
}