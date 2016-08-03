using System;
using System.Diagnostics;
using Chutzpah.Models;
using Chutzpah.Utility;

namespace Chutzpah.VS.Common
{
    public class VsDebuggerTestLauncher : ITestLauncher
    {
        readonly IUrlBuilder urlBuilder;

        public VsDebuggerTestLauncher(IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
        }

        public void LaunchTest(TestContext testContext)
        {
            var file = testContext.TestHarnessPath;
            file = urlBuilder.GenerateFileUrl(testContext, file, fullyQualified: true);

            // Start IE.
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = BrowserPathHelper.GetBrowserPath("ie"),
                Arguments = string.Format("-noframemerging -suspended -debug {0}", file)
                    // -noframemerging
                    //      This is what VS does when launching the script debugger.
                    //      Unsure whether strictly necessary.
                    // -suspended
                    //      This is what VS does when launching the script debugger.
                    //      Seems to cause IE to suspend all threads which is what we want.
                    // -debug
                    //      This is what VS does when launching the script debugger.
                    //      Not sure exactly what it does.
            };
            Process ieMainProcess = Process.Start(startInfo);

            // Get child 'tab' process spawned by IE.
            // We need to wait a few ms for IE to open the process.
            Process ieTabProcess = null;
            for (int i = 0;; ++i) {
                System.Threading.Thread.Sleep(10);
                ieTabProcess = ProcessExtensions.FindFirstChildProcessOf(ieMainProcess.Id);
                if (ieTabProcess != null) {
                    break; }
                if (i > 400) { // 400 * 10 = 4s timeout
                    throw new InvalidOperationException("Timeout waiting for Internet Explorer child process to start."); }
            }
                           
            // Debug the script in that tab process.
            DteHelpers.DebugAttachToProcess(ieTabProcess.Id, "script");

            // Resume the threads in the IE process which where started off suspended.
            ieTabProcess.Resume();
        }
    }
}
