using System;
using System.ComponentModel.Composition;
using Chutzpah.VS11.EventWatchers.EventArgs;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VS11.Plugin;

namespace Chutzpah.VS11.EventWatchers
{
	[Export(typeof(ISolutionEventsListener))]
    public class SolutionEventsListener : IVsSolutionEvents, ISolutionEventsListener
    {
        private readonly IVsSolution solution;
        private uint cookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// Fires an event when a project is opened/closed/loaded/unloaded
        /// </summary>
        public event EventHandler<SolutionEventsListenerEventArgs> SolutionChanged;

        [ImportingConstructor]
        public SolutionEventsListener([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
        {
            ValidateArg.NotNull(serviceProvider, "serviceProvider");
            this.solution = serviceProvider.GetService<IVsSolution>(typeof(SVsSolution));
        }

        public void StartListeningForChanges()
        {
            if (this.solution != null)
            {
                int hr = this.solution.AdviseSolutionEvents(this, out cookie);
                ErrorHandler.ThrowOnFailure(hr); // do nothing if this fails
            }
        }

        public void StopListeningForChanges()
        {
            if (this.cookie != VSConstants.VSCOOKIE_NIL && this.solution != null)
            {
                int hr = this.solution.UnadviseSolutionEvents(cookie);
                ErrorHandler.Succeeded(hr); // do nothing if this fails

                this.cookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        public void OnSolutionUpdated(IVsProject project, SolutionChangedReason reason)
        {
            if (this.SolutionChanged != null && project != null)
            {
                this.SolutionChanged(this, new SolutionEventsListenerEventArgs(project, reason));
            }
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            var project = pRealHierarchy as IVsProject;
            OnSolutionUpdated(project, SolutionChangedReason.Load);
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            var project = pRealHierarchy as IVsProject;
            OnSolutionUpdated(project, SolutionChangedReason.Unload);
            return VSConstants.S_OK;
        }


        // Unused events...

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }
    }

}