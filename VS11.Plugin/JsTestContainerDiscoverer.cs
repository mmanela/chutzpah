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
    [Export(typeof (ITestContainerDiscoverer))]
    public class JsTestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ITestRunner testRunner;
        private ISolutionEventsListener solutionListener;
        private ITestFilesUpdateWatcher testFilesUpdateWatcher;
        private ITestFileAddRemoveListener testFilesAddRemoveListener;
        private bool fullContainerSearchNeeded;
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
            :this(
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
            fullContainerSearchNeeded = true;
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
        /// This is the push notification VS uses to update the unit test window
        /// </summary>
        private void OnTestContainersChanged()
        {
            if (TestContainersUpdated != null)
            {
                TestContainersUpdated(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// The solution was unloaded so we need to indicate that next time containers are requested we do a full search
        /// </summary>
        private void SolutionListenerOnSolutionUnloaded(object sender, EventArgs eventArgs)
        {
            fullContainerSearchNeeded = true;
        }


        /// <summary>
        /// Handler to react to project load/unload events.
        /// </summary>
        private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            if (e != null)
            {
                var containers = FindTestContainers(e.Project);
                if (e.ChangedReason == SolutionChangedReason.Load)
                {
                    UpdateFileWatcher(containers, true);
                }
                else if (e.ChangedReason == SolutionChangedReason.Unload)
                {
                    UpdateFileWatcher(containers, false);
                }
            }

            // Do not fire OnTestContainersChanged here.
            // This will cause us to fire this event too early before the UTE is ready to process containers and will result in an exception.
            // The UTE will query all the TestContainerDiscoverers once the solution is loaded.
        }

        /// <summary>
        /// Handler to react to test file Add/remove/rename andcontents changed events
        /// </summary>
        private void OnProjectItemChanged(object sender, TestFileChangedEventArgs e)
        {
            if (e != null)
            {
                switch (e.ChangedReason)
                {
                    case TestFileChangedReason.Added:
                        testFilesUpdateWatcher.AddWatch(e.File);
                        if(IsTestFile(e.File))
                        {
                            AddOrUpdateTestContainer(new JsTestContainer(this, e.File, Constants.ExecutorUri));
                        }
                        break;
                    case TestFileChangedReason.Removed:
                        testFilesUpdateWatcher.RemoveWatch(e.File);                        
                        if(IsTestFile(e.File))
                        {
                            RemoveTestContainer(new JsTestContainer(this, e.File, Constants.ExecutorUri));
                        }
                        break;
                    case TestFileChangedReason.Changed:
                        if(IsTestFile(e.File))
                        {
                            AddOrUpdateTestContainer(new JsTestContainer(this, e.File, Constants.ExecutorUri));
                        }
                        break;
                }

                OnTestContainersChanged();
            }
        }

        /// <summary>
        /// After a project is loaded or unloaded either add or remove from the file watcher
        /// all test items inside that project
        /// </summary>
        private void UpdateFileWatcher(IEnumerable<ITestContainer> containers, bool isAdd)
        {
            foreach (var container in containers)
            {
                if (isAdd)
                {
                    testFilesUpdateWatcher.AddWatch(container.Source);
                    AddOrUpdateTestContainer(container);
                }
                else
                {
                    testFilesUpdateWatcher.RemoveWatch(container.Source);
                    RemoveTestContainer(container);
                }
            }
        }

        private void AddOrUpdateTestContainer(ITestContainer container)
        {
            RemoveTestContainer(container);
            cachedContainers.Add(container);
        }

        private void RemoveTestContainer(ITestContainer container)
        {
            var index = cachedContainers.FindIndex(x => x.Source.Equals(container.Source, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                cachedContainers.RemoveAt(index);
            }
        }

        private IEnumerable<ITestContainer> GetTestContainers()
        {
            if(fullContainerSearchNeeded)
            {
                cachedContainers = FindTestContainers();
                fullContainerSearchNeeded = false;
            }

            return cachedContainers;
        } 

        private List<ITestContainer> FindTestContainers()
        {
            var solution = (IVsSolution) serviceProvider.GetService(typeof (SVsSolution));
            var loadedProjects = solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

            return loadedProjects.SelectMany(FindTestContainers).ToList();
        }

        private IEnumerable<ITestContainer> FindTestContainers(IVsProject project)
        {
            return from item in VsSolutionHelper.GetProjectItems(project)
                   where IsTestFile(item)
                   select new JsTestContainer(this, item, Constants.ExecutorUri);

        }

        private bool IsTestFile(string path)
        {
            return ".js".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase) && testRunner.IsTestFile(path);
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