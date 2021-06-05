using System.Diagnostics;
using Chutzpah.Models;

namespace Chutzpah
{
    /// <summary>
    /// Describes a custom test launch service.
    /// </summary>
    public interface ITestLauncher
    {
        Process DebuggingProcess { get; set; }

        void LaunchTest(TestContext testContext);
    }
}
