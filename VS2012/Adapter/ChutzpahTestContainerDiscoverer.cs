using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chutzpah.Extensions;
using Chutzpah.Models;
using Chutzpah.VS.Common;
using Chutzpah.VS11.EventWatchers;
using Chutzpah.VS11.EventWatchers.EventArgs;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using VS11.Plugin;
using ILogger = Chutzpah.VS.Common.ILogger;

namespace Chutzpah.VS2012.TestAdapter
{
    public class TestFileCandidate
    {
        public TestFileCandidate()
        {

        }

        public TestFileCandidate(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
    }

    [Export(typeof(ITestContainerDiscoverer))]
    [Export(typeof(ChutzpahTestContainerDiscoverer))]
    public class ChutzpahTestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IChutzpahSettingsMapper settingsMapper;
        private readonly ILogger logger;
        private readonly ITestRunner testRunner;
        private readonly IFileProbe fileProbe;
        private readonly IChutzpahTestSettingsService chutzpahTestSettingsService;
        private ISolutionEventsListener solutionListener;
        private ITestFilesUpdateWatcher testFilesUpdateWatcher;
        private ITestFileAddRemoveListener testFilesAddRemoveListener;

        private object sync = new object();

        /// <summary>
        /// This is set to true one initial plugin load and then reset to false when a 
        /// solution is unloaded.  This will imply a full container refresh as well as
        /// supress asking the containers to refresh until the initial search finishedd
        /// </summary>
        private bool initialContainerSearch;

        /// <summary>
        /// This is set to true when a chutzpah.json file changes. This will set a flag that
        /// will cause a full container refresh
        /// </summary>
        private bool forceFullContainerRefresh;

        private readonly ConcurrentDictionary<string, ITestContainer> cachedContainers;

        public event EventHandler TestContainersUpdated;

        public Uri ExecutorUri
        {
            get { return AdapterConstants.ExecutorUri; }
        }

        public IEnumerable<ITestContainer> TestContainers
        {
            get { return GetTestContainers(); }
        }


        [ImportingConstructor]
        public ChutzpahTestContainerDiscoverer(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
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
                ChutzpahContainer.Get<IFileProbe>(),
                ChutzpahContainer.Get<IChutzpahTestSettingsService>())
        {
        }

        public ChutzpahTestContainerDiscoverer(IServiceProvider serviceProvider,
                                               IChutzpahSettingsMapper settingsMapper,
                                               ILogger logger,
                                               ISolutionEventsListener solutionListener,
                                               ITestFilesUpdateWatcher testFilesUpdateWatcher,
                                               ITestFileAddRemoveListener testFilesAddRemoveListener,
                                               ITestRunner testRunner,
                                               IFileProbe fileProbe,
                                               IChutzpahTestSettingsService chutzpahTestSettingsService)
        {
            initialContainerSearch = true;
            cachedContainers = new ConcurrentDictionary<string, ITestContainer>(StringComparer.OrdinalIgnoreCase);
            this.serviceProvider = serviceProvider;
            this.settingsMapper = settingsMapper;
            this.logger = logger;
            this.testRunner = testRunner;
            this.fileProbe = fileProbe;
            this.chutzpahTestSettingsService = chutzpahTestSettingsService;
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

            ChutzpahTracer.TraceInformation("Begin OnTestContainersChanged");
            if (TestContainersUpdated != null && !initialContainerSearch)
            {
                TestContainersUpdated(this, EventArgs.Empty);
            }
            ChutzpahTracer.TraceInformation("End OnTestContainersChanged");
        }


        /// <summary>
        /// The solution was unloaded so we need to indicate that next time containers are requested we do a full search
        /// </summary>
        private void SolutionListenerOnSolutionUnloaded(object sender, EventArgs eventArgs)
        {

            ChutzpahTracer.TraceInformation("Solution Unloaded...");
            initialContainerSearch = true;
        }


        /// <summary>
        /// Handler to react to project load/unload events.
        /// </summary>
        private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            if (e != null)
            {
                string projectPath = VsSolutionHelper.GetProjectPath(e.Project);

                var files = FindPotentialTestFiles(e.Project);
                if (e.ChangedReason == SolutionChangedReason.Load)
                {
                    ChutzpahTracer.TraceInformation("Project Loaded: '{0}'", projectPath);
                    UpdateChutzpahEnvironmentForProject(projectPath);
                    UpdateTestContainersAndFileWatchers(files, true);
                }
                else if (e.ChangedReason == SolutionChangedReason.Unload)
                {
                    ChutzpahTracer.TraceInformation("Project Unloaded: '{0}'", projectPath);
                    RemoveChutzpahEnvironmentForProject(projectPath);
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
        private void UpdateTestContainersAndFileWatchers(IEnumerable<TestFileCandidate> files, bool isAdd)
        {

            ChutzpahTracer.TraceInformation("Begin UpdateTestContainersAndFileWatchers");
            Parallel.ForEach(files, file =>
            {
                try
                {
                    if (isAdd)
                    {

                        ChutzpahTracer.TraceInformation("Adding watch on {0}", file.Path);
                        testFilesUpdateWatcher.AddWatch(file.Path);
                        AddTestContainerIfTestFile(file);
                    }
                    else
                    {
                        ChutzpahTracer.TraceInformation("Removing watch on {0}", file.Path);
                        testFilesUpdateWatcher.RemoveWatch(file.Path);
                        RemoveTestContainer(file);
                    }
                }
                catch (Exception e)
                {
                    ChutzpahTracer.TraceError(e, "Failed in UpdateTestContainersAndFileWatchers");
                }
            });

            ChutzpahTracer.TraceInformation("End UpdateTestContainersAndFileWatchers");
        }


        /// <summary>
        /// Handler to react to test file Add/remove/rename andcontents changed events
        /// </summary>
        private void OnProjectItemChanged(object sender, TestFileChangedEventArgs e)
        {

            ChutzpahTracer.TraceInformation("Begin OnProjectItemChanged");
            if (e != null)
            {

                // If a chutzpah.json file changed then we set the flag to 
                // ensure next time get
                if (fileProbe.IsChutzpahSettingsFile(e.File.Path))
                {
                    forceFullContainerRefresh = true;
                    return;
                }

                // Don't do anything for files we are sure can't be test files
                if (!HasTestFileExtension(e.File.Path)) return;

                logger.Log(string.Format("Changed detected for {0} with change type of {1}", e.File, e.ChangedReason),
                           "ChutzpahTestContainerDiscoverer",
                           LogType.Information);

                switch (e.ChangedReason)
                {
                    case TestFileChangedReason.Added:
                        ChutzpahTracer.TraceInformation("Adding watch on {0}", e.File.Path);
                        testFilesUpdateWatcher.AddWatch(e.File.Path);
                        AddTestContainerIfTestFile(e.File);

                        break;
                    case TestFileChangedReason.Removed:
                        ChutzpahTracer.TraceInformation("Removing watch on {0}", e.File.Path);
                        testFilesUpdateWatcher.RemoveWatch(e.File.Path);
                        RemoveTestContainer(e.File);

                        break;
                    case TestFileChangedReason.Changed:
                        AddTestContainerIfTestFile(e.File);
                        break;
                }

                OnTestContainersChanged();
            }

            ChutzpahTracer.TraceInformation("End OnProjectItemChanged");
        }

        /// <summary>
        /// Adds a test container for the given file if it is a test file.
        /// This will first remove any existing container for that file
        /// </summary>
        /// <param name="file"></param>
        private void AddTestContainerIfTestFile(TestFileCandidate file)
        {
            // If a settings file don't add a container
            if (fileProbe.IsChutzpahSettingsFile(file.Path))
            {
                return;
            }

            var isTestFile = IsTestFile(file.Path);

            RemoveTestContainer(file); // Remove if there is an existing container

            if (isTestFile)
            {

                ChutzpahTracer.TraceInformation("Added test container for '{0}'", file.Path);
                var container = new JsTestContainer(this, file.Path.ToLowerInvariant(), AdapterConstants.ExecutorUri);
                cachedContainers[container.Source] = container;
            }

        }

        /// <summary>
        /// Will remove a test container for a given file path
        /// </summary>
        /// <param name="file"></param>
        private void RemoveTestContainer(TestFileCandidate file)
        {
            // If a settings file don't add a container
            if (fileProbe.IsChutzpahSettingsFile(file.Path))
            {
                return;
            }

            ITestContainer container;
            var res = cachedContainers.TryRemove(file.Path, out container);
            if (res)
            {
                ChutzpahTracer.TraceInformation("Removed test container for '{0}'", file.Path);
            }
        }

        private IEnumerable<ITestContainer> GetTestContainers()
        {
            ChutzpahTracer.TraceInformation("Begin GetTestContainers");
            logger.Log("GetTestContainers() are called", "ChutzpahTestContainerDiscoverer", LogType.Information);

            ChutzpahTracingHelper.Toggle(settingsMapper.Settings.EnabledTracing);

            if (initialContainerSearch || forceFullContainerRefresh)
            {

                ChutzpahTracer.TraceInformation("Begin Initial test container search");
                logger.Log("Initial test container search", "ChutzpahTestContainerDiscoverer", LogType.Information);

                // Before the full container search we clear the settings cache to make sure 
                // we are getting the latest version of the settings
                // If the user changes the settings file after this it will cause a full search again
                chutzpahTestSettingsService.ClearCache();

                cachedContainers.Clear();

                var jsFiles = FindPotentialTestFiles();
                UpdateTestContainersAndFileWatchers(jsFiles, true);
                initialContainerSearch = false;
                forceFullContainerRefresh = false;

                ChutzpahTracer.TraceInformation("End Initial test container search");
            }


            ChutzpahTracer.TraceInformation("End GetTestContainers");
            return cachedContainers.Values;
        }

        private IEnumerable<TestFileCandidate> FindPotentialTestFiles()
        {
            try
            {
                ChutzpahTracer.TraceInformation("Begin enumerating loaded projects for test files");
                var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
                var loadedProjects = solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

                return loadedProjects.SelectMany(FindPotentialTestFiles).ToList();
            }
            finally
            {
                ChutzpahTracer.TraceInformation("End enumerating loaded projects for test files");
            }
        }

        private IEnumerable<TestFileCandidate> FindPotentialTestFiles(IVsProject project)
        {
            string projectPath = VsSolutionHelper.GetProjectPath(project);
            UpdateChutzpahEnvironmentForProject(projectPath);

            try
            {

                ChutzpahTracer.TraceInformation("Begin selecting potential test files from project '{0}'", projectPath);
                return (from item in VsSolutionHelper.GetProjectItems(project)
                        let hasTestExtension = HasTestFileExtension(item)
                        let isChutzpahSettingsFile = fileProbe.IsChutzpahSettingsFile(item)
                        where !fileProbe.IsTemporaryChutzpahFile(item) && (hasTestExtension || isChutzpahSettingsFile)
                        select new TestFileCandidate
                        {
                            Path = item
                        }).ToList();
            }
            finally
            {
                ChutzpahTracer.TraceInformation("End selecting potential test files from project '{0}'", projectPath);
            }
        }

        private void RemoveChutzpahEnvironmentForProject(string projectPath)
        {
            lock (sync)
            {
                var dirPath = Path.GetDirectoryName(projectPath);
                var envProps = settingsMapper.Settings.ChutzpahSettingsFileEnvironments
                                             .FirstOrDefault(x => x.Path.TrimEnd('/', '\\').Equals(dirPath, StringComparison.OrdinalIgnoreCase));

                settingsMapper.Settings.ChutzpahSettingsFileEnvironments.Remove(envProps);
            }

        }

        private void UpdateChutzpahEnvironmentForProject(string projectPath)
        {
            var buildProject = ProjectCollection.GlobalProjectCollection.LoadedProjects
                                                .FirstOrDefault(x => x.FullPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase));

            var chutzpahEnvProps = new Collection<ChutzpahSettingsFileEnvironmentProperty>();

            if (buildProject != null)
            {
                var dirPath = Path.GetDirectoryName(projectPath);
                foreach (var prop in ChutzpahMsBuildProps.GetProps())
                {
                    var value = buildProject.GetPropertyValue(prop);
                    if (!string.IsNullOrEmpty(value))
                    {
                        chutzpahEnvProps.Add(new ChutzpahSettingsFileEnvironmentProperty(prop, value));
                    }
                }

                lock (sync)
                {
                    var envProps = settingsMapper.Settings.ChutzpahSettingsFileEnvironments
                                                          .FirstOrDefault(x => x.Path.TrimEnd('/', '\\').Equals(dirPath, StringComparison.OrdinalIgnoreCase));

                    if (envProps == null && chutzpahEnvProps.Any())
                    {
                        envProps = new ChutzpahSettingsFileEnvironment(dirPath);

                        settingsMapper.Settings.ChutzpahSettingsFileEnvironments.Add(envProps);

                    }

                    if (envProps != null)
                    {
                        envProps.Properties = chutzpahEnvProps;
                    }
                }
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
                return HasTestFileExtension(path) && testRunner.IsTestFile(path, settingsMapper.Settings.ChutzpahSettingsFileEnvironmentsWrapper);
            }
            catch (Exception e)
            {
                ChutzpahTracer.TraceError(e, "ChutzpahTestContainerDiscoverer::Error when detecting a test file");
                logger.Log("Error when detecting a test file", "ChutzpahTestContainerDiscoverer", e);
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