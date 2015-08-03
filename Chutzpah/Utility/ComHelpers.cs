using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Chutzpah.Utility
{
    [
        ComImport, 
        Guid("00000016-0000-0000-C000-000000000046"), 
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    internal interface IMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller,
            int dwTickCount,
            IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwRejectType);

        [PreserveSig]
        int MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwPendingType);
    }

    public class ResilientMessageFilter : IMessageFilter
    {

    #region IMessageFilter Members

        int IMessageFilter.HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller, 
            int dwTickCount, 
            IntPtr lpInterfaceInfo)
        {
        // This method applicable to calls arriving in this (STA) thd.
        //
            return /*SERVERCALL_ISHANDLED*/0;
        }

        int IMessageFilter.RetryRejectedCall(
            IntPtr hTaskCallee, 
            int dwTickCount, 
            int dwRejectType)
        {
            // If server asked us to retry later...tell COM to retry later, with any delay
            // according to current dwTickCount.
            if (dwRejectType == /*SERVERCALL_RETRYLATER*/2
                && dwTickCount < 10000) { // timeout on retrying after 10s
                Debug.Assert(dwTickCount != -1);
                return dwTickCount / 10 +1;
            }
            // Owise our call to the server apartment was rejected by the server so tell COM
            // we're happy to abandon.
            else {
                return -1; }
        }

        int IMessageFilter.MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount, 
            int dwPendingType)
        {
        // Called when windows message arrives while this (STA) thd is calling out 
        // to another apartment.
        //
            return /*PENDINGMSG_WAITDEFPROCESS*/2;
        }

    #endregion

    }

    /// <summary>
    /// Helper class for establishing the registration of the ResilientMessageFilter class
    /// for a limited scope. To be used with the using() pattern.
    /// </summary>
    public class ResilientMessageFilterScope : IDisposable
    {
        [DllImport("Ole32.dll")]
        private static extern int CoRegisterMessageFilter(
            IMessageFilter newFilter, 
            out IMessageFilter orgFilter);

        IMessageFilter orgFilter = null;

        public ResilientMessageFilterScope()
        {
            IMessageFilter newFilter = new ResilientMessageFilter();
            CoRegisterMessageFilter(newFilter, out orgFilter);
        }

        public void Dispose()
        {
            IMessageFilter prevFilter = null;
            CoRegisterMessageFilter(orgFilter, out prevFilter);
        }
    }
}
