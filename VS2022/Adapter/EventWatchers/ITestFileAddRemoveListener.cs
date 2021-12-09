using System;
using Chutzpah.VS11.EventWatchers.EventArgs;

namespace Chutzpah.VS11.EventWatchers
{
    public interface ITestFileAddRemoveListener
    {
        event EventHandler<TestFileChangedEventArgs> TestFileChanged;
        void StartListeningForTestFileChanges();
        void StopListeningForTestFileChanges();
    }
}