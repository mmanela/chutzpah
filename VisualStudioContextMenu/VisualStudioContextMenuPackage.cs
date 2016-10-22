using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Chutzpah.Callbacks;
using Chutzpah.VS.Common;
using Chutzpah.VS.Common.Settings;
using Chutzpah.VisualStudio.Callback;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DTEConstants = EnvDTE.Constants;
using Task = System.Threading.Tasks.Task;
using Chutzpah.Coverage;
using Microsoft.Build.Evaluation;
using Chutzpah.Models;

namespace Chutzpah.VisualStudioContextMenu
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
    [ProvideBindingPath]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", Constants.ChutzpahVersion, IconResourceID = 400)]
    [ProvideOptionPage(typeof(ChutzpahSettings), "Chutzpah", "Chutzpah Settings", 111, 113, true)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [Guid(GuidList.guidChutzpahPkgString)]
    public sealed class ChutzpahPackage : Package
    {
        private DTE2 dte;
        private ITestRunner testRunner;
        internal ILogger Logger { get; private set; }
        private ITestMethodRunnerCallback runnerCallback;
        private IVsStatusbar statusBar;
        private IProcessHelper processHelper;
        private readonly object syncLock = new object();
        private bool testingInProgress;

        private SolutionEventsListener solutionListener;

        private ChutzpahSettingsFileEnvironments settingsEnvironments = new ChutzpahSettingsFileEnvironments();

        public ChutzpahSettings Settings { get; private set; }

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ChutzpahPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by Visual Studio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            dte = (DTE2)GetService(typeof(DTE));
            if (dte == null)
            {
                //if dte is null then we throw a exception
                //this is a fatal error
                throw new ArgumentNullException("dte");
            }

            testRunner = TestRunner.Create();
            
            processHelper = ChutzpahContainer.Get<IProcessHelper>();
            Logger = new Logger(this);
            Settings = GetDialogPage(typeof(ChutzpahSettings)) as ChutzpahSettings;

            statusBar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            runnerCallback = new ParallelRunnerCallbackAdapter(new VisualStudioRunnerCallback(dte, statusBar));


            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Command - Run JS Tests
                var runJsTestsCmd = new CommandID(GuidList.guidChutzpahCmdSet, (int)PkgCmdIDList.cmdidRunJSTests);
                var runJsTestMenuCmd = new OleMenuCommand(RunJSTestCmdCallback, runJsTestsCmd);
                runJsTestMenuCmd.BeforeQueryStatus += RunJSTestsCmdQueryStatus;
                mcs.AddCommand(runJsTestMenuCmd);

                // Command - Run JS tests in browser
                var runJsTestsInBrowserCmd = new CommandID(GuidList.guidChutzpahCmdSet, (int)PkgCmdIDList.cmdidRunInBrowser);
                var runJsTestInBrowserMenuCmd = new OleMenuCommand(RunJSTestInBrowserCmdCallback, runJsTestsInBrowserCmd);
                runJsTestInBrowserMenuCmd.BeforeQueryStatus += RunJSTestsInBrowserCmdQueryStatus;
                mcs.AddCommand(runJsTestInBrowserMenuCmd);

                // Command - Run Code Coverage
                var runJsTestCodeCoverageCmd = new CommandID(GuidList.guidChutzpahCmdSet, (int)PkgCmdIDList.cmdidRunCodeCoverage);
                var runJsTestCodeCoverageMenuCmd = new OleMenuCommand(RunCodeCoverageCmdCallback, runJsTestCodeCoverageCmd);
                runJsTestCodeCoverageMenuCmd.BeforeQueryStatus += RunCodeCoverageCmdQueryStatus;
                mcs.AddCommand(runJsTestCodeCoverageMenuCmd);


                var runJsTestDebuggerCmd = new CommandID(GuidList.guidChutzpahCmdSet, (int)PkgCmdIDList.cmdidDebugTests);
                var runJsTestDebuggerMenuCmd = new OleMenuCommand(RunDebuggerCmdCallback, runJsTestDebuggerCmd);
                runJsTestDebuggerMenuCmd.BeforeQueryStatus += RunDebuggerCmdQueryStatus;
                mcs.AddCommand(runJsTestDebuggerMenuCmd);

            }


            this.solutionListener = new SolutionEventsListener(this);
            this.solutionListener.SolutionUnloaded += OnSolutionUnloaded;
            this.solutionListener.SolutionProjectChanged += OnSolutionProjectChanged;
            this.solutionListener.StartListeningForChanges();

        }

        private void InitializeSettingsFileEnvironments()
        {
            var newEnvironments = new ChutzpahSettingsFileEnvironments();
            var buildProjects = ProjectCollection.GlobalProjectCollection.LoadedProjects
                                        .Where(x => !x.FullPath.Equals(".user", StringComparison.OrdinalIgnoreCase));

            foreach (var buildProject in buildProjects)
            {
                var dirPath = buildProject.DirectoryPath;
                var environment = new ChutzpahSettingsFileEnvironment(dirPath);
                foreach (var prop in ChutzpahMsBuildProps.GetProps())
                {
                    var value = buildProject.GetPropertyValue(prop);
                    if (!string.IsNullOrEmpty(value))
                    {
                        environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty(prop, value));
                    }
                }

                if (environment.Properties.Any())
                {
                    newEnvironments.AddEnvironment(environment);
                }
            }

            settingsEnvironments = newEnvironments;
        }

        private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            if (e.ChangedReason == SolutionChangedReason.Load || e.ChangedReason == SolutionChangedReason.Unload)
            {
                InitializeSettingsFileEnvironments();
            }
        }

        private void OnSolutionUnloaded(object sender, EventArgs e)
        {
            settingsEnvironments = null;
        }

        private TestFileType GetFileType(EnvDTE.ProjectItem item)
        {
            if (IsFile(item))
            {
                var filename = item.FileNames[0];
                return GetFileType(filename);
            }

            if (IsFolder(item))
            {
                return TestFileType.Folder;
            }

            return TestFileType.Other;
        }


        private TestFileType GetFileType(string filename)
        {
            if (filename.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".coffee", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                )
            {
                return TestFileType.JS;
            }

            if (filename.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return TestFileType.HTML;
            }

            return TestFileType.Other;
        }

        private void RunJSTestInBrowserCmdCallback(object sender, EventArgs e)
        {
            CheckTracing();

            IEnumerable<string> selectedFiles = null;
            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                // We only support one file for opening in browser through VS for now
                selectedFiles = SearchForTestableFiles().Take(1);
            }
            else if (activeWindow.Kind == "Document")
            {
                selectedFiles = new List<string> { CurrentDocumentPath };
            }

            RunTests(selectedFiles, openInBrowser: true);
        }

        private void RunJSTestCmdCallback(object sender, EventArgs e)
        {
            CheckTracing();

            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                RunTestsInSolutionFolderNodeCallback(sender, e);
            }
            else if (activeWindow.Kind == "Document")
            {
                RunTestsFromEditorCallback(sender, e);
            }
        }

        private void RunCodeCoverageCmdCallback(object sender, EventArgs e)
        {
            CheckTracing();

            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                RunTestsInSolutionFolderNodeCallback(sender, e, withCodeCoverage: true);
            }
            else if (activeWindow.Kind == "Document")
            {
                RunTestsFromEditorCallback(sender, e, withCodeCoverage: true);
            }
        }


        private void RunDebuggerCmdCallback(object sender, EventArgs e)
        {
            CheckTracing();

            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                RunTestsInSolutionFolderNodeCallback(sender, e, withDebugger: true);
            }
            else if (activeWindow.Kind == "Document")
            {
                RunTestsFromEditorCallback(sender, e, withDebugger: true);
            }
        }



        private void RunJSTestsCmdQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            SetCommandVisibility(menuCommand, TestFileType.Folder, TestFileType.HTML, TestFileType.JS);
        }

        private void RunJSTestsInBrowserCmdQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            SetCommandVisibility(menuCommand, TestFileType.HTML, TestFileType.JS);
        }

        private void RunCodeCoverageCmdQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            SetCommandVisibility(menuCommand, TestFileType.Folder, TestFileType.JS);
        }
        private void RunDebuggerCmdQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            SetCommandVisibility(menuCommand, TestFileType.Folder, TestFileType.HTML, TestFileType.JS);
        }


        private void SetCommandVisibility(OleMenuCommand menuCommand, params TestFileType[] allowedTypes)
        {
            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                Array activeItems = SolutionExplorerItems;
                foreach (UIHierarchyItem item in activeItems)
                {
                    var solutionItem = (item).Object as EnvDTE.Solution;
                    var projectItem = (item).Object as EnvDTE.ProjectItem;
                    var projectNode = (item).Object as EnvDTE.Project;

                    TestFileType fileType = TestFileType.Other;
                    if (projectItem != null)
                    {
                        fileType = GetFileType(projectItem);
                    }
                    else if (projectNode != null)
                    {
                        fileType = TestFileType.Folder;
                    }
                    else if (solutionItem != null)
                    {
                        fileType = TestFileType.Folder;
                    }

                    if (!allowedTypes.Contains(fileType))
                    {
                        menuCommand.Visible = false;
                        return;
                    }
                }
            }
            else if (activeWindow.ObjectKind == DTEConstants.vsDocumentKindText)
            {
                var fileType = GetFileType(activeWindow.Document.FullName);
                if (!allowedTypes.Contains(fileType))
                {
                    menuCommand.Visible = false;
                    return;
                }
            }

            menuCommand.Visible = true;
        }

        private void RunTestsInSolutionFolderNodeCallback(object sender, EventArgs e, bool withCodeCoverage = false, bool withDebugger = false)
        {
            var filePaths = GetSelectedFilesAndFolders(TestFileType.Folder, TestFileType.HTML, TestFileType.JS);
            RunTests(filePaths, withCodeCoverage: withCodeCoverage, withDebugger: withDebugger);
        }

        private void RunTestsFromEditorCallback(object sender, EventArgs e, bool withCodeCoverage = false, bool withDebugger = false)
        {
            string filePath = CurrentDocumentPath;
            RunTests(filePath, withCodeCoverage: withCodeCoverage, withDebugger: withDebugger);
        }

        private void RunTests(string filePath, bool openInBrowser = false, bool withCodeCoverage = false, bool withDebugger = false)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                RunTests(new[] { filePath }, openInBrowser: openInBrowser, withCodeCoverage: withCodeCoverage, withDebugger: withDebugger);
            }
        }

        private void RunTests(IEnumerable<string> filePaths, bool openInBrowser = false, bool withCodeCoverage = false, bool withDebugger = false)
        {
            if (!testingInProgress)
            {
                lock (syncLock)
                {
                    if (!testingInProgress)
                    {
                        dte.Documents.SaveAll();
                        var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                        testingInProgress = true;

                        Task.Factory.StartNew(
                            () =>
                            {
                                try
                                {
                                    // If settings file environments have not yet been initialized, do so here.
                                    if (settingsEnvironments == null || settingsEnvironments.Count == 0)
                                    {
                                        InitializeSettingsFileEnvironments();
                                    }

                                    var options = new TestOptions
                                                      {
                                                          MaxDegreeOfParallelism = Settings.MaxDegreeOfParallelism,
                                                          CoverageOptions = new CoverageOptions
                                                          {
                                                              Enabled = withCodeCoverage
                                                          },

                                                          CustomTestLauncher = withDebugger ? ChutzpahContainer.Get<VsDebuggerTestLauncher>() : null,
                                                          TestLaunchMode = GetTestLaunchMode(openInBrowser, withDebugger),
                                                          ChutzpahSettingsFileEnvironments = settingsEnvironments
                                                      };
                                    var result = testRunner.RunTests(filePaths, options, runnerCallback);

                                    if (result.CoverageObject != null)
                                    {
                                        var path = CoverageOutputGenerator.WriteHtmlFile(Path.Combine(solutionDir, Constants.CoverageHtmlFileName), result.CoverageObject);
                                        processHelper.LaunchLocalFileInBrowser(path);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.Log("Error while running tests", "ChutzpahPackage", e);
                                }
                                finally
                                {
                                    testingInProgress = false;
                                }
                            });
                    }
                }
            }
        }

        private static TestLaunchMode GetTestLaunchMode(bool openInBrowser, bool withDebugger)
        {
            if (openInBrowser)
            {
                return TestLaunchMode.FullBrowser;
            }
            else if (withDebugger)
            {
                return TestLaunchMode.Custom;
            }
            else
            {
                return TestLaunchMode.HeadlessBrowser;
            }
        }

        private List<string> GetSelectedFilesAndFolders(params TestFileType[] allowedTypes)
        {
            var filePaths = new List<string>();
            foreach (object item in SolutionExplorerItems)
            {
                var solutionItem = ((UIHierarchyItem)item).Object as EnvDTE.SolutionClass;
                var projectItem = ((UIHierarchyItem)item).Object as EnvDTE.ProjectItem;
                var projectNode = ((UIHierarchyItem)item).Object as EnvDTE.Project;

                if (solutionItem != null)
                {
                    foreach (EnvDTE.Project subItem in solutionItem.Projects)
                    {
                        if (!string.IsNullOrEmpty(subItem.FullName))
                        {
                            filePaths.Add(Path.GetDirectoryName(subItem.FullName));
                        }
                    }
                }
                else if (projectItem != null)
                {
                    string filePath = projectItem.FileNames[0];
                    var type = GetFileType(projectItem);
                    if (allowedTypes.Contains(type))
                    {
                        filePaths.Add(filePath);
                    }
                }
                else if (projectNode != null)
                {
                    filePaths.Add(Path.GetDirectoryName(projectNode.FullName));
                }
            }
            return filePaths;
        }

        private IEnumerable<string> SearchForTestableFiles()
        {
            Predicate<TestFileType> fileTypeCheck = x => x == TestFileType.JS || x == TestFileType.HTML;
            var filePaths = new List<string>();
            foreach (object item in SolutionExplorerItems)
            {
                var projectItem = ((UIHierarchyItem)item).Object as EnvDTE.ProjectItem;
                if (projectItem != null)
                {
                    string filePath = projectItem.FileNames[0];
                    TestFileType fileType = GetFileType(projectItem);
                    if (fileTypeCheck(fileType))
                        filePaths.Add(filePath);
                }
            }

            return filePaths;
        }

        private Array SolutionExplorerItems
        {
            get
            {
                var hierarchy = (UIHierarchy)dte.ToolWindows.GetToolWindow(DTEConstants.vsWindowKindSolutionExplorer);
                return (Array)hierarchy.SelectedItems;
            }
        }

        private IEnumerable<string> GetFilesFromTree(ProjectItems projectItems, Predicate<TestFileType> validFile)
        {
            var files = new List<string>();
            GetFilesFromTree(projectItems, validFile, files);
            return files;
        }

        private void GetFilesFromTree(ProjectItems projectItems,
                                      Predicate<TestFileType> validFile,
                                      List<string> filePaths)
        {
            foreach (EnvDTE.ProjectItem projectItem in projectItems)
            {
                if (IsFile(projectItem))
                {
                    string filePath = projectItem.FileNames[0];
                    if (validFile(GetFileType(projectItem)))
                        filePaths.Add(filePath);
                }
                else if (projectItem.ProjectItems != null && projectItem.ProjectItems.Count > 0)
                    GetFilesFromTree(projectItem.ProjectItems, validFile, filePaths);
            }
        }

        private bool IsFile(EnvDTE.ProjectItem projectItem)
        {
            try
            {
                return projectItem.Kind.Equals(DTEConstants.vsProjectItemKindPhysicalFile);
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to determine if project item is file", "ChutzpahPackage", ex);
            }

            return false;
        }


        private bool IsFolder(EnvDTE.ProjectItem projectItem)
        {
            try
            {
                return projectItem.Kind.Equals(DTEConstants.vsProjectItemKindPhysicalFolder);
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to determine if project item is folder", "ChutzpahPackage", ex);
            }

            return false;
        }

        /// <summary>
        /// get the current text document
        /// if current documents isn't a text doc return null
        /// </summary>
        internal TextDocument CurrentTextDocument
        {
            get
            {
                try
                {
                    return GetTextDocumentFromWindow(dte.ActiveWindow);
                }
                catch (Exception e)
                {
                    Logger.Log("Error getting active window", "ChutzpahPackage", e);
                    return null;
                }
            }
        }

        internal string CurrentDocumentPath
        {
            get
            {
                try
                {
                    return dte.ActiveWindow.Document.FullName;
                }
                catch (Exception e)
                {
                    Logger.Log("Error getting active document path", "ChutzpahPackage", e);
                    return null;
                }
            }
        }

        internal TextDocument GetTextDocumentFromWindow(Window window)
        {
            TextDocument codeDoc = null;
            if (dte != null)
            {
                Document doc = null;
                try
                {
                    doc = window.Document;
                }
                catch (ArgumentException)
                {
                    return null; //error occurred return null
                }

                if (doc != null)
                {
                    codeDoc = doc.Object(String.Empty) as TextDocument;
                }
            }
            return codeDoc;
        }

        public void CheckTracing()
        {
            var path = Path.Combine(Path.GetTempPath(), Chutzpah.Constants.LogFileName);
            if (Settings.EnabledTracing)
            {
                ChutzpahTracer.AddFileListener(path);
            }
            else
            {
                ChutzpahTracer.RemoveFileListener(path);
            }
        }
    }
}