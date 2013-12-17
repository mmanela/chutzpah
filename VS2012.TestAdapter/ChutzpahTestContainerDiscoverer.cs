using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Chutzpah.Extensions;
using Chutzpah.Models;
using Chutzpah.VS.Common;
using Chutzpah.VS11.EventWatchers;
using Chutzpah.VS11.EventWatchers.EventArgs;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using VS11.Plugin;
using ILogger = Chutzpah.VS.Common.ILogger;

namespace Chutzpah.VS2012.TestAdapter
{
    [Export(typeof (ITestContainerDiscoverer))]
    public class ChutzpahTestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IChutzpahSettingsMapper settingsMapper;
        private readonly ILogger logger;
        private readonly ITestRunner testRunner;
        private readonly IFileProbe fileProbe;
        private ISolutionEventsListener solutionListener;
        private ITestFilesUpdateWatcher testFilesUpdateWatcher;
        private ITestFileAddRemoveListener testFilesAddRemoveListener;
        private bool initialContainerSearch;
        private readonly List<ITestContainer> cachedContainers;

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
        public ChutzpahTestContainerDiscoverer(
            [Import(typeof (SVsServiceProvider))] IServiceProvider serviceProvider,
            IChutzpahSettingsMapper settingsMapper,
            ISolutionEventsListener solutionListener,
            ITestFilesUpdateWatcher testFilesUpdateWatcher,
            ITestFileAddRemoveListener testFilesAddRemoveListener)
            : this(
                serviceProvider,
                settingsMapper,
                new Logger(serviceProvider),
                solutionListener,
                testFilesUpdateWatcher,
                testFilesAddRemoveListener,
                TestRunner.Create(),
                ChutzpahContainer.Get<IFileProbe>())
        {
        }

        public ChutzpahTestContainerDiscoverer(IServiceProvider serviceProvider,
                                               IChutzpahSettingsMapper settingsMapper,
                                               ILogger logger,
                                               ISolutionEventsListener solutionListener,
                                               ITestFilesUpdateWatcher testFilesUpdateWatcher,
                                               ITestFileAddRemoveListener testFilesAddRemoveListener,
                                               ITestRunner testRunner,
                                               IFileProbe fileProbe)
        {
            initialContainerSearch = true;
            cachedContainers = new List<ITestContainer>();
            this.serviceProvider = serviceProvider;
            this.settingsMapper = settingsMapper;
            this.logger = logger;
            this.testRunner = testRunner;
            this.fileProbe = fileProbe;
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
                var files = FindPotentialTestFiles(e.Project);
                if (e.ChangedReason == SolutionChangedReason.Load)
                {
                    UpdateTestContainersAndFileWatchers(files, true);
                }
                else if (e.ChangedReason == SolutionChangedReason.Unload)
                {
                    UpdateTestContainersAndFileWatchers(files, false);
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
        private void UpdateTestContainersAndFileWatchers(IEnumerable<string> files, bool isAdd)
        {

            ChutzpahTracer.TraceInformation("Begin UpdateTestContainersAndFileWatchers");
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

            ChutzpahTracer.TraceInformation("End UpdateTestContainersAndFileWatchers");
        }


        /// <summary>
        /// Handler to react to test file Add/remove/rename andcontents changed events
        /// </summary>
        private void OnProjectItemChanged(object sender, TestFileChangedEventArgs e)
        {
            if (e != null)
            {
                // Don't do anything for files we are sure can't be test files
                if (!HasTestFileExtension(e.File)) return;
                logger.Log(string.Format("Changed detected for {0} with change type of {1}", e.File, e.ChangedReason),
                           "ChutzpahTestContainerDiscoverer",
                           LogType.Information);

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

            if (isTestFile)
            {

                ChutzpahTracer.TraceInformation("Added test container for '{0}'", file);
                var container = new JsTestContainer(this, file.ToLowerInvariant(), Constants.ExecutorUri);
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

                ChutzpahTracer.TraceInformation("Removed test container for '{0}'", file);
                cachedContainers.RemoveAt(index);
            }
        }

        private IEnumerable<ITestContainer> GetTestContainers()
        {
            ChutzpahTracer.TraceInformation("Begin GetTestContainers");
            logger.Log("GetTestContainers() are called", "ChutzpahTestContainerDiscoverer", LogType.Information);


            ChutzpahTracingHelper.Toggle(settingsMapper.Settings.EnabledTracing);

            if (initialContainerSearch)
            {

                ChutzpahTracer.TraceInformation("Begin Initial test container search");
                logger.Log("Initial test container search", "ChutzpahTestContainerDiscoverer", LogType.Information);

                cachedContainers.Clear();
                var jsFiles = FindPotentialTestFiles();
                UpdateTestContainersAndFileWatchers(jsFiles, true);
                initialContainerSearch = false;

                ChutzpahTracer.TraceInformation("End Initial test container search");
            }

            var containers = FilterContainers(cachedContainers);

            ChutzpahTracer.TraceInformation("End GetTestContainers");
            return containers;
        }

        private IEnumerable<ITestContainer> FilterContainers(IEnumerable<ITestContainer> containers)
        {
            var mode = settingsMapper.Settings.TestingMode;
            return containers.Where(x => mode.FileBelongsToTestingMode(x.Source));
        }

        private IEnumerable<string> FindPotentialTestFiles()
        {
            try
            {
                ChutzpahTracer.TraceInformation("Begin enumerating loaded projects for test files");
                var solution = (IVsSolution) serviceProvider.GetService(typeof (SVsSolution));
                var loadedProjects = solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

                return loadedProjects.SelectMany(FindPotentialTestFiles).ToList();
            }
            finally
            {
                ChutzpahTracer.TraceInformation("End enumerating loaded projects for test files");
            }
        }

        private IEnumerable<string> FindPotentialTestFiles(IVsProject project)
        {
            try
            {
                ChutzpahTracer.TraceInformation("Begin selecting potential test files from project");
                return (from item in VsSolutionHelper.GetProjectItems(project)
                    where HasTestFileExtension(item) && !fileProbe.IsTemporaryChutzpahFile(item)
                    select item).ToList();
            }
            finally
            {
                ChutzpahTracer.TraceInformation("End selecting potential test files from project");
            }
        }


        private static bool HasTestFileExtension(string path)
        {
            return TestingMode.All.FileBelongsToTestingMode(path);
        }

        private bool IsTestFile(string path)
        {
            try
            {
                return HasTestFileExtension(path) && testRunner.IsTestFile(path);
            }
            catch (IOException e)
            {
                logger.Log("IO error when detecting a test file", "ChutzpahTestContainerDiscoverer", e);
            }

            return false;
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
                    ((IDisposable) testFilesUpdateWatcher).Dispose();
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