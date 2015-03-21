using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Chutzpah.VS.Common.Settings
{
    /// <summary>
    /// General options page
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("5A1369BC-C7CE-4176-9080-84BA3F6125AB")]
    public class ChutzpahSettings : DialogPage
    {
        public ChutzpahSettings()
        {
            // Set default value, these will get overridden when user changes them
            MaxDegreeOfParallelism = Environment.ProcessorCount;
        }

        [Browsable(true)]
        [Category("General")]
        [DisplayName("Max degree of Parallelism")]
        [Description("The maximum amount of concurreny Chutzpah should use")]
        public int MaxDegreeOfParallelism { get; set; }

        [Browsable(true)]
        [Category("General")]
        [DisplayName("Enable Chutzpah Tracing")]
        [Description("Saves a trace of Chutzpah execution to %temp%\\chutzpah.log")]
        public bool EnabledTracing { get; set; }

        public override void ResetSettings()
        {
            EnabledTracing = false;
            MaxDegreeOfParallelism = Environment.ProcessorCount;
            base.ResetSettings();
        }

    }
}