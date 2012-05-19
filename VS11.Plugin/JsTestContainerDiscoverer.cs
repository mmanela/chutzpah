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
            Debugger.Break();
            this.serviceProvider = serviceProvider;
            this.testRunner = testRunner;
            this.solutionListener = solutionListener;
            this.testFilesUpdateWatcher = testFilesUpdateWatcher;
            this.testFilesAddRemoveListener = testFilesAddRemoveListener;


            this.testFilesAddRemoveListener.TestFileChanged += OnProjectItemChanged;
            this.testFilesAddRemoveListener.StartListeningForTestFileChanges();

            this.solutionListener.SolutionChanged += OnSolutionChanged;
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
        /// Handler to react to project load/unload events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSolutionChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            if (e != null)
            {
                var containers = GetTestContainers(e.Project);
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
                        break;
                    case TestFileChangedReason.Removed:
                        testFilesUpdateWatcher.RemoveWatch(e.File);
                        break;
                    default:
                        //In changed case file watcher observed a file changed event
                        //In this case we just have to fire TestContainerChnaged event
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
                }
                else
                {
                    testFilesUpdateWatcher.RemoveWatch(container.Source);
                }
            }
        }


        private IEnumerable<ITestContainer> GetTestContainers()
        {
            var solution = (IVsSolution) serviceProvider.GetService(typeof (SVsSolution));
            var loadedProjects = solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

            return loadedProjects.SelectMany(GetTestContainers);
        }

        private IEnumerable<ITestContainer> GetTestContainers(IVsProject project)
        {
            return from item in VsSolutionHelper.GetProjectItems(project)
                   where ".js".Equals(Path.GetExtension(item), StringComparison.OrdinalIgnoreCase)
                         && testRunner.IsTestFile(item)
                   select new JsTestContainer(this, item, Constants.ExecutorUri);

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
                    solutionListener.SolutionChanged -= OnSolutionChanged;
                    solutionListener.StopListeningForChanges();
                    solutionListener = null;
                }
            }
        }
    }
}