using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Management;

namespace Chutzpah.Utility
{
    public static class ProcessExtensions
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        // [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public int ExitStatus;
            public int PebBaseAddress;
            public int AffinityMask;
            public int BasePriority;
            public uint UniqueProcessId;
            public uint InheritedFromUniqueProcessId;
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             int processId);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(
            ThreadAccess dwDesiredAccess,
            bool bInheritHandle,
            uint dwThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(
            IntPtr hObject);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(
            IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(
            IntPtr hThread);
        [DllImport("ntdll.dll")]
        static extern int NtQueryInformationProcess(
            IntPtr hProcess,
            int processInformationClass /* 0 */,
            ref PROCESS_BASIC_INFORMATION processBasicInformation,
            uint processInformationLength,
            out uint returnLength);

        /// <summary>
        /// Helper to suspened all threads in the specified process.
        /// </summary>
        public static void Suspend(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread != IntPtr.Zero)
                {
                    SuspendThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
            }
        }

        /// <summary>
        /// Helper to resume any/all suspended threads in the specified process.
        /// </summary>
        public static void Resume(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread != IntPtr.Zero)
                {
                    ResumeThread(pOpenThread);
                    CloseHandle(pOpenThread);
                }
            }
        }

        /// <summary>
        /// Helper that returns the first process in the collection of system processes
        /// that is identified as a child of the specified process.
        /// </summary>
        public static Process FindFirstChildProcessOf(int subjectProcessId)
        {
            // Retrieve all processes on the system
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                // Get some basic information about the process
                PROCESS_BASIC_INFORMATION processInfoB = new PROCESS_BASIC_INFORMATION();
                try
                {
                    uint bytesWritten;
                    int ntStatus = NtQueryInformationProcess(
                        p.Handle,
                        0,
                        ref processInfoB,
                        (uint)Marshal.SizeOf(processInfoB),
                        out bytesWritten); // == 0 is OK
                    if (0 != ntStatus)
                    { // fail?
                        continue;
                    }
                    // Is it a child process of the subject process?
                    if (processInfoB.InheritedFromUniqueProcessId == subjectProcessId)
                    {
                        return p;
                    }
                }
                catch (Exception /* ex */)
                {
                    // Ignore, most likely 'Access Denied'
                }
            }
            return null;
        }

        /// <summary>
        /// Locate the first child process for the specified process id.
        /// </summary>
        /// <param name="parentProcessId"></param>
        /// <returns></returns>
        public static Process FindFirstChildProcess(int parentProcessId)
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.management.managementobjectsearcher.get?view=dotnet-plat-ext-6.0
            //https://social.msdn.microsoft.com/Forums/en-US/81c8137e-6868-4b95-bdf8-37624b7f0166/managementobjectsearcher-processes-win32process-access-denied?forum=csharplanguage
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessId = " + parentProcessId);
            ManagementObjectCollection resultList = searcher.Get();

            try
            {
                foreach (ManagementObject item in resultList)
                {
                    object value = item.GetPropertyValue("ProcessId");
                    int childProcessId = Convert.ToInt32(value);
                    return Process.GetProcessById(childProcessId);
                }
            }
            catch { }

            return null;

        }

        /// <summary>
        /// Helper that tests process parent-child inheritance.
        /// </summary>
        /// <param name="matchSame">Value to return if/when process ids are the same.</param>
        public static bool IsProcessAAncestorOfProcessB(
            int processId_A,
            int processId_B,
            bool matchSame = true,
            int maxHops = int.MaxValue)
        {
            if (processId_A == processId_B)
            {
                return matchSame;
            }
            // Get process handle from the process id.
            //Process pB = Process.GetProcessById(processId_B); // This throws if process not found, which is undesirable.
            var hProcess = OpenProcess(ProcessAccessFlags.QueryInformation, false, processId_B);
            if (hProcess == IntPtr.Zero)
            {
                return false;
            }
            // Get info about the process.
            PROCESS_BASIC_INFORMATION processInfoB = new PROCESS_BASIC_INFORMATION();
            uint bytesWritten;
            int ntStatus = NtQueryInformationProcess(
                hProcess,
                0,
                ref processInfoB,
                (uint)Marshal.SizeOf(processInfoB),
                out bytesWritten); // == 0 is OK
            CloseHandle(hProcess);
            if (0 != ntStatus)
            {
                Debug.Assert(false);
                return false;
            }
            // Does B directly inherit from A?
            if (processInfoB.InheritedFromUniqueProcessId == processId_A)
            {
                return true;
            }
            // Optionally recurse up the hierarchy.
            if (maxHops > 0)
            {
                if (processInfoB.InheritedFromUniqueProcessId != 0)
                {
                    return IsProcessAAncestorOfProcessB(
                        processId_A,
                        (int)processInfoB.InheritedFromUniqueProcessId,
                        matchSame,
                        maxHops - 1);
                }
            }
            // No match.
            return false;
        }
    }
}
