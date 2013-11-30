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
        public ChutzpahUTESettings()
        {
            maxDegreeOfParallelism = 1;
        }


        private TestingMode? testingMode;
        private int? timeoutMilliseconds;
        private int maxDegreeOfParallelism;
        private bool enableTracing;


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
        [Description("Should the Unit Test Explorer scan JavaScript, TypeScript, CoffeeScript or HTML files or both?")]
        public TestingMode TestingMode
        {
            get { return testingMode ?? TestingMode.AllExceptHTML; }
            set
            {
                testingMode = value;
                OnPropertyChanged("TestingMode");
            }
        }

        [Browsable(true)]
        [Category("UTE")]
        [DisplayName("Max degree of Parallelism")]
        [Description("The maximum amount of concurreny Chutzpah should use")]
        public int MaxDegreeOfParallelism
        {
            get { return maxDegreeOfParallelism; }
            set
            {
                maxDegreeOfParallelism = value;
                OnPropertyChanged("MaxDegreeOfParallelism");
            }
        }


        [Browsable(true)]
        [Category("UTE")]
        [DisplayName("Enable Chutzpah Tracing")]
        [Description("Saves a trace of Chutzpah execution to %temp%\\chutzpah.log")]
        public bool EnabledTracing
        {
            get { return enableTracing; }
            set
            {
                enableTracing = value;
                OnPropertyChanged("EnabledTracing");
            }
        }

        public override void ResetSettings()
        {
            EnabledTracing = false;
            TimeoutMilliseconds = null;
            TestingMode = TestingMode.JavaScript;
            MaxDegreeOfParallelism = 1;
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