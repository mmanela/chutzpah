using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;

namespace Chutzpah
{
    /// <summary>
    /// Describes a custom test launch service.
    /// </summary>
    public interface ITestLauncher
    {
        void LaunchTest(TestContext testContext);
    }
}
