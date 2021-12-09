using System;
using Chutzpah.VS11.EventWatchers.EventArgs;

namespace Chutzpah.VS11.EventWatchers
{
    public interface ITestFilesUpdateWatcher
    {
        event EventHandler<TestFileChangedEventArgs> FileChangedEvent;
        void AddWatch(string path);
        void RemoveWatch(string path);
    }
}