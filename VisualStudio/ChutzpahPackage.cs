using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Chutzpah.VisualStudio.RunnerCallback;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = EnvDTE.Constants;
using Task = System.Threading.Tasks.Task;
using System.Linq;
using Chutzpah.Models;
using Chutzpah.Wrappers;

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
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
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
        IProcessHelper processHelper;
        private readonly object syncLock = new object();
        private bool testingInProgress;

        // Keeps track of last staging folder
        // This will allow the open in browser path to stay the same so
        // you can just hit refresh
        string stagingFolder;

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

            processHelper = new ProcessHelper();
            Logger = new Logger(this);
            statusBar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            runnerCallback = new VisualStudioRunnerCallback(dte, statusBar);


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
                runJsTestInBrowserMenuCmd.BeforeQueryStatus += RunJSTestsCmdQueryStatus;
                mcs.AddCommand(runJsTestInBrowserMenuCmd);
            }
        }

        private static TestFileType GetFileType(string filename)
        {
            if (filename.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                return TestFileType.JS;
            }


            if (filename.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return TestFileType.HTML;
            }

            if (filename.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return TestFileType.Folder;
            }

            return TestFileType.Other;
        }

        private void RunJSTestInBrowserCmdCallback(object sender, EventArgs e)
        {
            string selectedFile = null;
            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == EnvDTE.Constants.vsWindowKindSolutionExplorer)
            {
                selectedFile = GetSelectedTestableFiles().FirstOrDefault();
            }
            else if (activeWindow.Kind == "Document")
            {
                selectedFile = CurrentDocumentPath;
            }

            if (selectedFile != null)
            {
                try
                {
                    var testContext = testRunner.GetTestContext(selectedFile, new TestOptions { StagingFolder = stagingFolder });
                    stagingFolder = Path.GetDirectoryName(testContext.TestHarnessPath);
                    LaunchFileInBrowser(testContext.TestHarnessPath);
                }
                catch (FileNotFoundException)
                { }
            }
        }

        private void RunJSTestCmdCallback(object sender, EventArgs e)
        {
            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == EnvDTE.Constants.vsWindowKindSolutionExplorer)
            {
                RunTestsInSolutionFolderNodeCallback(sender, e);
            }
            else if (activeWindow.Kind == "Document")
            {
                RunTestsFromEditorCallback(sender, e);
            }
        }

        private void RunJSTestsCmdQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            var activeWindow = dte.ActiveWindow;
            if (activeWindow.ObjectKind == EnvDTE.Constants.vsWindowKindSolutionExplorer)
            {

                Array activeItems = SolutionExplorerItems;
                foreach (UIHierarchyItem item in activeItems)
                {
                    TestFileType fileType = GetFileType(item.Name);

                    if (fileType == TestFileType.Other || fileType == TestFileType.Folder)
                    {
                        menuCommand.Visible = false;

                        return;
                    }
                }
            }

            menuCommand.Visible = true;
        }

        private void RunTestsInSolutionFolderNodeCallback(object sender, EventArgs e)
        {
            var filePaths = GetSelectedTestableFiles();
            RunTests(filePaths);
        }

        private void RunTestsFromEditorCallback(object sender, EventArgs e)
        {
            string filePath = CurrentDocumentPath;
            RunTests(filePath);
        }

        private void RunTests(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                RunTests(new[] { filePath });
            }
        }

        private void RunTests(IEnumerable<string> filePaths)
        {
            if (!testingInProgress)
            {
                lock (syncLock)
                {
                    if (!testingInProgress)
                    {
                        dte.Documents.SaveAll();
                        testingInProgress = true;
                        Task.Factory.StartNew(
                            () =>
                            {
                                var summary = testRunner.RunTests(filePaths, new TestOptions { StagingFolder = stagingFolder }, runnerCallback);
                                stagingFolder = Path.GetDirectoryName(summary.Tests.Last().HtmlTestFile);
                                testingInProgress = false;
                            });
                    }
                }
            }
        }

        private void LaunchFileInBrowser(string file)
        {
            processHelper.LaunchFileInBrowser(file);
        }

        private List<string> GetSelectedTestableFiles()
        {
            Predicate<TestFileType> fileTypeCheck = x => x == TestFileType.JS || x == TestFileType.HTML;
            var filePaths = new List<string>();
            foreach (object item in SolutionExplorerItems)
            {
                var projectItem = ((UIHierarchyItem)item).Object as ProjectItem;
                var projectNode = ((UIHierarchyItem)item).Object as Project;

                if (projectItem != null)
                {
                    string filePath = projectItem.FileNames[0];
                    TestFileType fileType = GetFileType(filePath);
                    if (fileTypeCheck(fileType))
                        filePaths.Add(filePath);
                    else
                        filePaths.AddRange(GetFilesFromTree(projectItem.ProjectItems, fileTypeCheck));
                }
                else if (projectNode != null)
                {
                    filePaths.AddRange(GetFilesFromTree(projectNode.ProjectItems, fileTypeCheck));
                }
            }
            return filePaths;
        }

        private Array SolutionExplorerItems
        {
            get
            {
                var hierarchy = (UIHierarchy)dte.ToolWindows.GetToolWindow(Constants.vsWindowKindSolutionExplorer);
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
                    if (validFile(GetFileType(filePath)))
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
                return projectItem.Kind.Equals(Constants.vsProjectItemKindPhysicalFile);
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to determine if project item is file", "ChutzpahPackage", ex);
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
    }
}