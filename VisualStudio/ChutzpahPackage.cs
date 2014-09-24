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

namespace Chutzpah.VisualStudio
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
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112",  Constants.ChutzpahVersion, IconResourceID = 400)]
    [ProvideOptionPage(typeof(ChutzpahSettings), "Chutzpah", "Chutzpah Settings", 110, 113, true)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [Guid(GuidList.guidChutzpahPkgString)]
    public sealed class ChutzpahPackage : Package
    {
        private DTE2 dte;
        private ITestRunner testRunner;
        private IChutzpahTestSettingsService chutzpahSettingsService;
        internal ILogger Logger { get; private set; }
        private ITestMethodRunnerCallback runnerCallback;
        private IVsStatusbar statusBar;
        private IProcessHelper processHelper;
        private readonly object syncLock = new object();
        private bool testingInProgress;

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
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            dte = (DTE2)GetService(typeof(DTE));
            if (dte == null)
            {
                //if dte is null then we throw a excpetion
                //this is a fatal error
                throw new ArgumentNullException("dte");
            }

            testRunner = TestRunner.Create();
            chutzpahSettingsService = ChutzpahContainer.Get<IChutzpahTestSettingsService>();

            processHelper = new ProcessHelper();
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

                // Command - Run JS tests in browser
                var runJsTestCodeCoverageCmd = new CommandID(GuidList.guidChutzpahCmdSet, (int)PkgCmdIDList.cmdidRunCodeCoverage);
                var runJsTestCodeCoverageMenuCmd = new OleMenuCommand(RunCodeCoverageCmdCallback, runJsTestCodeCoverageCmd);
                runJsTestCodeCoverageMenuCmd.BeforeQueryStatus += RunCodeCoverageCmdQueryStatus;
                mcs.AddCommand(runJsTestCodeCoverageMenuCmd);

            }
        }

        private TestFileType GetFileType(ProjectItem item)
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
                || filename.EndsWith(".coffee", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
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

            RunTests(selectedFiles, false, true);
        }

        private void RunJSTestCmdCallback(object sender, EventArgs e)
        {
            CheckTracing();

            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                RunTestsInSolutionFolderNodeCallback(sender, e, false);
            }
            else if (activeWindow.Kind == "Document")
            {
                RunTestsFromEditorCallback(sender, e, false);
            }
        }

        private void RunCodeCoverageCmdCallback(object sender, EventArgs e)
        {
            CheckTracing();

            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                RunTestsInSolutionFolderNodeCallback(sender, e, true);
            }
            else if (activeWindow.Kind == "Document")
            {
                RunTestsFromEditorCallback(sender, e, true);
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

        private void SetCommandVisibility(OleMenuCommand menuCommand, params TestFileType[] allowedTypes)
        {
            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == DTEConstants.vsWindowKindSolutionExplorer)
            {
                Array activeItems = SolutionExplorerItems;
                foreach (UIHierarchyItem item in activeItems)
                {
                    var projectItem = (item).Object as ProjectItem;
                    var projectNode = (item).Object as Project;

                    TestFileType fileType = TestFileType.Other;
                    if (projectItem != null)
                    {
                        fileType = GetFileType(projectItem);
                    }
                    else if (projectNode != null)
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

        private void RunTestsInSolutionFolderNodeCallback(object sender, EventArgs e, bool withCodeCoverage)
        {
            var filePaths = GetSelectedFilesAndFolders(TestFileType.Folder, TestFileType.HTML, TestFileType.JS);
            RunTests(filePaths, withCodeCoverage, false);
        }

        private void RunTestsFromEditorCallback(object sender, EventArgs e, bool withCodeCoverage)
        {
            string filePath = CurrentDocumentPath;
            RunTests(filePath, withCodeCoverage);
        }

        private void RunTests(string filePath, bool withCodeCoverage)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                RunTests(new[] { filePath }, withCodeCoverage, false);
            }
        }

        private void RunTests(IEnumerable<string> filePaths, bool withCodeCoverage, bool openInBrowser)
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
                                    var options = new TestOptions
                                                      {
                                                          TestFileTimeoutMilliseconds = Settings.TimeoutMilliseconds,
                                                          MaxDegreeOfParallelism = Settings.MaxDegreeOfParallelism,
                                                          CoverageOptions = new CoverageOptions
                                                          {
                                                              Enabled = withCodeCoverage
                                                          },
                                                          OpenInBrowser = openInBrowser
                                                      };
                                    var result = testRunner.RunTests(filePaths, options, runnerCallback);
 
                                    if (result.CoverageObject != null)
                                    {
                                        var path = CoverageOutputGenerator.WriteHtmlFile(solutionDir, result.CoverageObject);
                                        processHelper.LaunchFileInBrowser(path);
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


        private List<string> GetSelectedFilesAndFolders(params TestFileType[] allowedTypes)
        {
            var filePaths = new List<string>();
            foreach (object item in SolutionExplorerItems)
            {
                var projectItem = ((UIHierarchyItem)item).Object as ProjectItem;
                var projectNode = ((UIHierarchyItem)item).Object as Project;

                if (projectItem != null)
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
                var projectItem = ((UIHierarchyItem)item).Object as ProjectItem;
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
            foreach (ProjectItem projectItem in projectItems)
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

        private bool IsFile(ProjectItem projectItem)
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


        private bool IsFolder(ProjectItem projectItem)
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
        /// if current documents isnt a text doc return null
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
                    return null; //error occured return null
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