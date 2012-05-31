using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Chutzpah.VS11.EventWatchers;
using Chutzpah.VS11.EventWatchers.EventArgs;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using VS11.Plugin;

namespace Chutzpah.VS11
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class JsTestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ITestRunner testRunner;
        private ISolutionEventsListener solutionListener;
        private ITestFilesUpdateWatcher testFilesUpdateWatcher;
        private ITestFileAddRemoveListener testFilesAddRemoveListener;
        private bool initialContainerSearch;
        private List<ITestContainer> cachedContainers;

        public event EventHandler TestContainersUpdated;

        public Uri ExecutorUri
        {
            get { return Constants.ExecutorUri; }
        }

        public IEnumerable<ITestContainer> TestContainers
        {
            get { return GetTestContainers(); }
        }


        [ImportingConstructor]
        public JsTestContainerDiscoverer(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISolutionEventsListener solutionListener,
            ITestFilesUpdateWatcher testFilesUpdateWatcher,
            ITestFileAddRemoveListener testFilesAddRemoveListener)
            : this(
                serviceProvider,
                solutionListener,
                testFilesUpdateWatcher,
                testFilesAddRemoveListener,
                TestRunner.Create())
        {
        }

        public JsTestContainerDiscoverer(
            IServiceProvider serviceProvider,
            ISolutionEventsListener solutionListener,
            ITestFilesUpdateWatcher testFilesUpdateWatcher,
            ITestFileAddRemoveListener testFilesAddRemoveListener,
            ITestRunner testRunner)
        {
            initialContainerSearch = true;
            cachedContainers = new List<ITestContainer>();
            this.serviceProvider = serviceProvider;
            this.testRunner = testRunner;
            this.solutionListener = solutionListener;
            this.testFilesUpdateWatcher = testFilesUpdateWatcher;
            this.testFilesAddRemoveListener = testFilesAddRemoveListener;


            this.testFilesAddRemoveListener.TestFileChanged += OnProjectItemChanged;
            this.testFilesAddRemoveListener.StartListeningForTestFileChanges();

            this.solutionListener.SolutionUnloaded += SolutionListenerOnSolutionUnloaded;
            this.solutionListener.SolutionProjectChanged += OnSolutionProjectChanged;
            this.solutionListener.StartListeningForChanges();

            this.testFilesUpdateWatcher.FileChangedEvent += OnProjectItemChanged;
        }

        /// <summary>
        /// Fire Events to Notify testcontainerdiscoverer listeners that containers have changed.
        /// This is the push notification VS uses to update the unit test window.
        /// 
        /// The initialContainerSearch check is meant to prevent us from notifying VS about updates 
        /// until it is ready
        /// </summary>
        private void OnTestContainersChanged()
        {
            if (TestContainersUpdated != null && !initialContainerSearch)
            {
                TestContainersUpdated(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// The solution was unloaded so we need to indicate that next time containers are requested we do a full search
        /// </summary>
        private void SolutionListenerOnSolutionUnloaded(object sender, EventArgs eventArgs)
        {
            initialContainerSearch = true;
        }


        /// <summary>
        /// Handler to react to project load/unload events.
        /// </summary>
        private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            if (e != null)
            {
                var files = FindJsFiles(e.Project);
                if (e.ChangedReason == SolutionChangedReason.Load)
                {
                    UpdateFileWatcher(files, true);
                }
                else if (e.ChangedReason == SolutionChangedReason.Unload)
                {
                    UpdateFileWatcher(files, false);
                }
            }

            // Do not fire OnTestContainersChanged here.
            // This will cause us to fire this event too early before the UTE is ready to process containers and will result in an exception.
            // The UTE will query all the TestContainerDiscoverers once the solution is loaded.
        }



        /// <summary>
        /// After a project is loaded or unloaded either add or remove from the file watcher
        /// all test potential items inside that project
        /// </summary>
        private void UpdateFileWatcher(IEnumerable<string> files, bool isAdd)
        {
            foreach (var file in files)
            {
                if (isAdd)
                {
                    testFilesUpdateWatcher.AddWatch(file);
                    AddTestContainerIfTestFile(file);
                }
                else
                {
                    testFilesUpdateWatcher.RemoveWatch(file);
                    RemoveTestContainer(file);
                }
            }
        }


        /// <summary>
        /// Handler to react to test file Add/remove/rename andcontents changed events
        /// </summary>
        private void OnProjectItemChanged(object sender, TestFileChangedEventArgs e)
        {
            if (e != null)
            {
                // Don't do anything for files we are sure can't be test files
                if (!IsJsFile(e.File)) return;

                switch (e.ChangedReason)
                {
                    case TestFileChangedReason.Added:
                        testFilesUpdateWatcher.AddWatch(e.File);
                        AddTestContainerIfTestFile(e.File);

                        break;
                    case TestFileChangedReason.Removed:
                        testFilesUpdateWatcher.RemoveWatch(e.File);
                        RemoveTestContainer(e.File);

                        break;
                    case TestFileChangedReason.Changed:
                        AddTestContainerIfTestFile(e.File);
                        break;
                }

                OnTestContainersChanged();
            }
        }

        /// <summary>
        /// Adds a test container for the given file if it is a test file.
        /// This will first remove any existing container for that file
        /// </summary>
        /// <param name="file"></param>
        private void AddTestContainerIfTestFile(string file)
        {
            var isTestFile = IsTestFile(file);
            RemoveTestContainer(file); // Remove if there is an existing container

            // If this is a test file
            if (isTestFile)
            {
                var container = new JsTestContainer(this, file, Constants.ExecutorUri);
                cachedContainers.Add(container);
            }
        }

        /// <summary>
        /// Will remove a test container for a given file path
        /// </summary>
        /// <param name="file"></param>
        private void RemoveTestContainer(string file)
        {
            var index = cachedContainers.FindIndex(x => x.Source.Equals(file, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                cachedContainers.RemoveAt(index);
            }
        }

        private IEnumerable<ITestContainer> GetTestContainers()
        {
            if (initialContainerSearch)
            {
                cachedContainers.Clear();
                var jsFiles = FindJsFiles();
                UpdateFileWatcher(jsFiles, true);
                initialContainerSearch = false;
            }

            return cachedContainers;
        }

        private IEnumerable<string> FindJsFiles()
        {
            var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            var loadedProjects = solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

            return loadedProjects.SelectMany(FindJsFiles).ToList();
        }

        private IEnumerable<string> FindJsFiles(IVsProject project)
        {
            return from item in VsSolutionHelper.GetProjectItems(project)
                   where IsTestFile(item)
                   select item;
        }

        private static bool IsJsFile(string path)
        {
            return ".js".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsTestFile(string path)
        {
            return IsJsFile(path) && testRunner.IsTestFile(path);
        }

        public void Dispose()
        {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (testFilesUpdateWatcher != null)
                {
                    testFilesUpdateWatcher.FileChangedEvent -= OnProjectItemChanged;
                    ((IDisposable)testFilesUpdateWatcher).Dispose();
                    testFilesUpdateWatcher = null;
                }

                if (testFilesAddRemoveListener != null)
                {
                    testFilesAddRemoveListener.TestFileChanged -= OnProjectItemChanged;
                    testFilesAddRemoveListener.StopListeningForTestFileChanges();
                    testFilesAddRemoveListener = null;
                }

                if (solutionListener != null)
                {
                    solutionListener.SolutionProjectChanged -= OnSolutionProjectChanged;
                    solutionListener.StopListeningForChanges();
                    solutionListener = null;
                }
            }
        }
    }
}