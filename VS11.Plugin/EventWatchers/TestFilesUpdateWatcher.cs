using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Chutzpah.VS11.EventWatchers.EventArgs;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Chutzpah.VS11.EventWatchers
{
    [Export(typeof(ITestFilesUpdateWatcher))]
    public class TestFilesUpdateWatcher : IDisposable, ITestFilesUpdateWatcher
    {
        private IDictionary<string, FileSystemWatcher> fileWatchers;
        public event EventHandler<TestFileChangedEventArgs> FileChangedEvent;

        public TestFilesUpdateWatcher()
        {
            fileWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddWatch(string path)
        {
            ValidateArg.NotNullOrEmpty(path, "path");

            if (!String.IsNullOrEmpty(path))
            {
                var directoryName = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);

                FileSystemWatcher watcher;
                if (!fileWatchers.TryGetValue(path, out watcher))
                {
                    watcher = new FileSystemWatcher(directoryName, fileName);
                    fileWatchers.Add(path, watcher);

                    watcher.Changed += OnChanged;
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        public void RemoveWatch(string path)
        {
            ValidateArg.NotNullOrEmpty(path, "path");

            if (!String.IsNullOrEmpty(path))
            {
                FileSystemWatcher watcher;
                if (fileWatchers.TryGetValue(path, out watcher))
                {
                    watcher.EnableRaisingEvents = false;

                    fileWatchers.Remove(path);

                    watcher.Changed -= OnChanged;
                    watcher.Dispose();
                    watcher = null;
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (FileChangedEvent != null)
            {
                FileChangedEvent(sender, new TestFileChangedEventArgs(e.FullPath, TestFileChangedReason.Changed));
            }
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
            if (disposing && fileWatchers != null)
            {
                foreach (var fileWatcher in fileWatchers.Values)
                {
                    if (fileWatcher != null)
                    {
                        fileWatcher.Changed -= OnChanged;
                        fileWatcher.Dispose();
                    }
                }

                fileWatchers.Clear();
                fileWatchers = null;
            }
        }
    }
}