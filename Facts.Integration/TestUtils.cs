using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;

namespace Chutzpah.Facts.Integration
{
    public static class TestUtils
    {
        public static void RunAsJasmineVersionOne(Action action)
        {
            var version = ChutzpahTestSettingsFile.Default.FrameworkVersion;
            ChutzpahTestSettingsFile.Default.FrameworkVersion = "1";

            try
            {
                action();
            }
            finally
            {
                ChutzpahTestSettingsFile.Default.FrameworkVersion = version;
            }
        }
    }
}
