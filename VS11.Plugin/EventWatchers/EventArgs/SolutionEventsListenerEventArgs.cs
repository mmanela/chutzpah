using Microsoft.VisualStudio.Shell.Interop;

namespace Chutzpah.VS11.EventWatchers.EventArgs
{
    public enum SolutionChangedReason
    {
        None,
        Load,
        Unload,
    }


    public class SolutionEventsListenerEventArgs : System.EventArgs
    {
        public IVsProject Project { get; private set; }
        public SolutionChangedReason ChangedReason { get; private set; }

        public SolutionEventsListenerEventArgs(IVsProject project, SolutionChangedReason reason)
        {
            Project = project;
            ChangedReason = reason;
        }
    }
}