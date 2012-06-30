using System.ComponentModel;
using System.Runtime.InteropServices;
using Chutzpah.Models;
using Microsoft.VisualStudio.Shell;

namespace Chutzpah.VS.Common.Settings
{
    /// <summary>
    /// Chutzpah Adapter options page
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("6C95E241-763D-4887-9F05-A6B95F36D031")]
    public class ChutzpahUTESettings : DialogPage, INotifyPropertyChanged
    {
        private TestingMode? testingMode;
        private int? timeoutMilliseconds;


        [Browsable(true)]
        [Category("UTE")]
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
        [Category("UTE")]
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