using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Chutzpah.VS.Common;
using Chutzpah.VS.Common.Settings;
using Chutzpah.VS2012.TestAdapter;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using System.ComponentModel.Composition.Hosting;

namespace Chutzpah.VS2012
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideOptionPage(typeof (ChutzpahUTESettings), "Chutzpah", "Chutzpah Test Adapter Settings", 115, 116, true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", Constants.ChutzpahVersion, IconResourceID = 400)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [Guid(GuidList.guidVS2012ChutzpahPkgString)]
    public sealed class ChutzpahVS2012Package : Package
    {
        internal ILogger Logger { get; private set; }
        public ChutzpahUTESettings Settings { get; private set; }
        private IChutzpahSettingsMapper settingsMapper;

        private IComponentModel componentModel;

        public IComponentModel ComponentModel
        {
            get
            {
                if (componentModel == null)
                    componentModel = (IComponentModel) GetGlobalService(typeof (SComponentModel));
                return componentModel;
            }
        }


        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ChutzpahVS2012Package()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            Logger = new Logger(this);
            Settings = GetDialogPage(typeof (ChutzpahUTESettings)) as ChutzpahUTESettings;
            Settings.PropertyChanged += SettingsPropertyChanged;

            settingsMapper = ComponentModel.GetService<IChutzpahSettingsMapper>();
            settingsMapper.MapSettings(Settings);

        }

        private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            settingsMapper.MapSettings(Settings);
        }

        #endregion
    }
}