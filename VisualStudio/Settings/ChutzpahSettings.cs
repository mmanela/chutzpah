using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace Chutzpah.VisualStudio.Settings
{
    /// <summary>
    /// General options page
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("5A1369BC-C7CE-4176-9080-84BA3F6125AB")]
    public class ChutzpahSettings : DialogPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChutzpahSettings"/> class.
        /// </summary>
        /// <devdoc>
        /// Constructs the Dialog Page.
        /// </devdoc>
        public ChutzpahSettings()
        {
        }


        [Browsable(true)]
        [Category("General")]
        [DisplayName("Test file timeout (ms)")]
        [Description("How long to wait for a given test file to finish before timing out? (Defaults to 3000 ms)")]
        public int? TimeoutMilliseconds { get; set; }


        public override void ResetSettings()
        {
            TimeoutMilliseconds = null;
            base.ResetSettings();
        }

        //TIP 1: If you want to get access this option page from a VS Package use this snippet on the VsPackage class:
        //ChutzpahSettings optionPage = this.GetDialogPage(typeof(ChutzpahSettings)) as ChutzpahSettings;
    }
}
