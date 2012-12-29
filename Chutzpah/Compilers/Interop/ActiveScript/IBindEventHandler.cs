using System.Runtime.InteropServices;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Enables the caller to provide an object that handles a specified event handler.
    /// </summary>
    [Guid("63CDBCB0-C1B1-11d0-9336-00A0C90DCAA9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IBindEventHandler
    {
        /// <summary>
        /// Binds an event to an object.
        /// </summary>
        /// <param name="eventName">Specifies the event to handle.</param>
        /// <param name="dispatch">Specifies the object to handle the event.</param>
        void BindHandler(
            [MarshalAs(UnmanagedType.LPWStr)] string eventName,
            [MarshalAs(UnmanagedType.IDispatch)] object dispatch);
    }
}