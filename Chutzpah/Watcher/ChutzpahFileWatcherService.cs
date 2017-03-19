using Chutzpah.Models;
using Chutzpah.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chutzpah.Watcher
{

    public class ChutzpahFileWatcherService 
    {
        private ConcurrentDictionary<string, FileWatcherInfo> fileWatchers = new ConcurrentDictionary<string, FileWatcherInfo>(StringComparer.OrdinalIgnoreCase);
        public event EventHandler<FileSystemEventArgs> FileChangedEvent;
        private IFileSystemWrapper fileSystem;

        public ChutzpahFileWatcherService(IFileSystemWrapper fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void AddWatch(ChutzpahTestSettingsFile settings)
        {
            AddWatch(settings.SettingsFilePath);
        }

        public void AddWatch(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                var directoryName = fileSystem.GetDirectoryName(path);
                var fileName = fileSystem.GetFileName(path);

                FileWatcherInfo watcherInfo;
                if (!fileWatchers.TryGetValue(path, out watcherInfo))
                {
                    watcherInfo = new FileWatcherInfo(new FileSystemWatcher(directoryName, fileName));
                    if (fileWatchers.TryAdd(path, watcherInfo))
                    {

                        watcherInfo.Watcher.Changed += OnChanged;

                        // We are monitoring for this file to be renamed. 
                        // This is needed to catch file modifications in VS2013+. In these version VS won't update an existing file.
                        // It will create a new file, and then delete old one and swap in the new one transactionally
                        watcherInfo.Watcher.Renamed += OnRenamed;


                        watcherInfo.Watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            OnChanged(sender, renamedEventArgs);
        }

        public void RemoveWatch(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                FileWatcherInfo watcherInfo;
                if (fileWatchers.TryRemove(path, out watcherInfo))
                {
                    watcherInfo.Watcher.EnableRaisingEvents = false;

                    watcherInfo.Watcher.Changed -= OnChanged;
                    watcherInfo.Watcher.Dispose();
                    watcherInfo.Watcher = null;

                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            FileWatcherInfo watcherInfo = null;
            if (FileChangedEvent != null && fileWatchers.TryGetValue(e.FullPath, out watcherInfo))
            {
                var writeTime = File.GetLastWriteTime(e.FullPath);
                // Only fire update if enough time has passed since last update to prevent duplicate events
                if (writeTime.Subtract(watcherInfo.LastEventTime).TotalMilliseconds > 500)
                {
                    watcherInfo.LastEventTime = writeTime;
                    FileChangedEvent(sender, e);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && fileWatchers != null)
            {
                foreach (var fileWatcher in fileWatchers.Values)
                {
                    if (fileWatcher != null && fileWatcher.Watcher != null)
                    {
                        fileWatcher.Watcher.Changed -= OnChanged;
                        fileWatcher.Watcher.Dispose();
                    }
                }

                fileWatchers.Clear();
                fileWatchers = null;
            }
        }
        private class FileWatcherInfo
        {
            public FileWatcherInfo(FileSystemWatcher watcher)
            {
                Watcher = watcher;
                LastEventTime = DateTime.MinValue;
            }

            public FileSystemWatcher Watcher { get; set; }
            public DateTime LastEventTime { get; set; }
        }

    }
}
