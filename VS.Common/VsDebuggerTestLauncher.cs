using System;
using System.Diagnostics;
using Chutzpah.Models;
using Chutzpah.Utility;

namespace Chutzpah.VS.Common
{
    public class VsDebuggerTestLauncher : ITestLauncher
    {
        readonly IUrlBuilder urlBuilder;

        public Process DebuggingProcess { get; set; }

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

            int ieBrowserTabOpenTimeout = ((testContext.TestFileSettings.IEBrowserTabOpenTimeout ?? Constants.DefaultIEBrowserTabOpenTimeout) * 100);

            // Get child 'tab' process spawned by IE.
            for (int i = 0;; ++i)
            {
                // We need to wait a few ms for IE to open the process.
                System.Threading.Thread.Sleep(10);

                this.DebuggingProcess = ProcessExtensions.FindFirstChildProcessOf(ieMainProcess.Id);
                if (this.DebuggingProcess != null)
                {
                    break;
                }

                if (i > ieBrowserTabOpenTimeout)
                {
                    throw new InvalidOperationException("Timeout waiting for Internet Explorer child process to start.");
                }
            }
                           
            // Debug the script in that tab process.
            DteHelpers.DebugAttachToProcess(this.DebuggingProcess.Id, "script");

            // Resume the threads in the IE process which where started off suspended.
            this.DebuggingProcess.Resume();
        }
    }
}
