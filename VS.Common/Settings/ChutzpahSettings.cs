using System.ComponentModel;
using System.Runtime.InteropServices;
using Chutzpah.Models;
using Microsoft.VisualStudio.Shell;

namespace Chutzpah.VS.Common.Settings
{
    /// <summary>
    /// General options page
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("5A1369BC-C7CE-4176-9080-84BA3F6125AB")]
    public class ChutzpahSettings : DialogPage, INotifyPropertyChanged
    {
        private TestingMode? testingMode;
        private int? timeoutMilliseconds;


        [Browsable(true)]
        [Category("General")]
        [DisplayName("Test timeout (ms)")]
        [Description("How long to wait for a given test to finish before timing out? (Defaults to 5000 ms)")]
        public int? TimeoutMilliseconds
        {
            get { return timeoutMilliseconds; }
            set
            {
                timeoutMilliseconds = value;
                OnPropertyChanged("TimeoutMilliseconds");
            }
        }

        [Browsable(true)]
        [Category("Visual Studio 2012 Test Adapter")]
        [DisplayName("Testing Mode")]
        [Description("Should the Unit Test Explorer scan JavaScript, HTML files or both?")]
        public TestingMode TestingMode
        {
            get { return testingMode ?? Models.TestingMode.JavaScript; }
            set
            {
                testingMode = value;
                OnPropertyChanged("TestingMode");
            }
        }


        public override void ResetSettings()
        {
            TimeoutMilliseconds = null;
            TestingMode = TestingMode.JavaScript;
            base.ResetSettings();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}