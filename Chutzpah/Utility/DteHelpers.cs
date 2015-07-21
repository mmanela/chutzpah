using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Chutzpah.Utility
{
    public class DteHelpers
    {
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
 
        private static Regex s_regex_dteMoniker = new Regex(@"!VisualStudio\.DTE\.(?:\d|\.)+:(\d+)", RegexOptions.CultureInvariant);
            // Regex for matching the VS DTE moniker as stored in the COM running object table.
            // Note the process id suffix to the string which we extract as capture group 1.
            // Example target: "!VisualStudio.DTE.11.0:11944"

        /// <summary>
        /// Returns collection of any/all EnvDTE.DTE instances running on the machine.
        /// http://blogs.msdn.com/b/kirillosenkov/archive/2011/08/10/how-to-get-dte-from-visual-studio-process-id.aspx
        /// </summary>
        /// <returns>Collection of EnvDTE.DTE instances keyed by the ID of the process running the DTE.</returns>
        public static Dictionary<int, EnvDTE.DTE> GetAllDTEs()
        {
            Dictionary<int, EnvDTE.DTE> dtes = new Dictionary<int,EnvDTE.DTE>();
 
            IBindCtx bindCtx = null;
            IRunningObjectTable rot = null;
            IEnumMoniker enumMonikers = null;
 
            try
            {
                Marshal.ThrowExceptionForHR(CreateBindCtx(reserved: 0, ppbc: out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMonikers);
 
                IMoniker[] moniker = new IMoniker[1];
                IntPtr numberFetched = IntPtr.Zero;
                while (enumMonikers.Next(1, moniker, numberFetched) == 0)
                {
                    IMoniker runningObjectMoniker = moniker[0];
 
                    string name = null;
 
                    try
                    {
                        if (runningObjectMoniker != null)
                        {
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Do nothing, there is something in the ROT that we do not have access to.
                    }
 
                    // Parse the moniker to match against target spec. and extract process id.
                    int processId;
                    Match match = s_regex_dteMoniker.Match(name);
                    if (!match.Success
                        || match.Groups.Count != 2) {
                        continue; }
                    processId = int.Parse(match.Groups[1].Value);
                    
                    // Store the DTE.
                    object runningObject = null;
                    Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out runningObject));
                    dtes[processId] = (EnvDTE.DTE)runningObject;
                }
            }
            finally
            {
                if (enumMonikers != null)
                {
                    Marshal.ReleaseComObject(enumMonikers);
                }
 
                if (rot != null)
                {
                    Marshal.ReleaseComObject(rot);
                }
 
                if (bindCtx != null)
                {
                    Marshal.ReleaseComObject(bindCtx);
                }
            }
 
            return dtes;
        }

    }
}
