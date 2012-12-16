using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Chutzpah.Wrappers;

namespace Chutzpah.Utility
{
    public class CompilerCache : ICompilerCache
    {
        private readonly ConcurrentDictionary<string, Tuple<DateTime, string>> compilerCache;
        private readonly Hasher hasher;
        private readonly IFileSystemWrapper filesystem;
        private readonly IBinarySerializer binarySerializer;
        private readonly string filename;
        private bool disposed;

        public CompilerCache(IFileSystemWrapper fileSystem, IBinarySerializer binarySerializer)
        {
            hasher = new Hasher();
            compilerCache = new ConcurrentDictionary<string, Tuple<DateTime, string>>();
            filesystem = fileSystem;
            this.binarySerializer = binarySerializer;
            if (string.IsNullOrEmpty(GlobalOptions.Instance.CompilerCacheFile))
            {
                filename = Path.Combine(filesystem.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder),
                                        Constants.ChutzpahCompilerCacheFileName);
            }
            else
            {
                filename = GlobalOptions.Instance.CompilerCacheFile;
            }

            if (fileSystem.FileExists(filename) && GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes > 0)
            {
                using (var cacheStream = fileSystem.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    compilerCache = DeserializeObject(cacheStream);
                }
            }
        }

        public string Get(string source)
        {
            if (GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes <= 0) return null;

            var hash = hasher.Hash(source);
            Tuple<DateTime, string> cachedEntry;
            compilerCache.TryGetValue(hash, out cachedEntry);
            return cachedEntry == null ? null : cachedEntry.Item2;
        }

        public void Set(string source, string compiled)
        {
            if (GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes <= 0) return;

            var hash = hasher.Hash(source);
            var cachedEntry = Tuple.Create(DateTime.UtcNow, compiled);
            compilerCache.TryAdd(hash, cachedEntry);
        }

        public void Save()
        {
            LimitSize();
            SerializeObject(filename, compilerCache);
        }

        private void LimitSize()
        {
            var size = compilerCache.Sum(tuple => tuple.Value.Item2.Length);
            while (size > GlobalOptions.Instance.CompilerCacheFileMaxSizeBytes)
            {
                var oldestKey = "";
                var oldestTime = DateTime.UtcNow;
                foreach (var tuple in compilerCache)
                {
                    if (tuple.Value.Item1 < oldestTime)
                    {
                        oldestTime = tuple.Value.Item1;
                        oldestKey = tuple.Key;
                    }
                }
                size -= compilerCache[oldestKey].Item2.Length;

                Tuple<DateTime, string> deleted;
                compilerCache.TryRemove(oldestKey, out deleted);
            }
        }

        private void SerializeObject(string name,
                                     ConcurrentDictionary<string, Tuple<DateTime, string>> objectToSerialize)
        {
            try
            {
                using (var stream = filesystem.Open(name, FileMode.Create, FileAccess.Write))
                {
                    binarySerializer.Serialize(stream, objectToSerialize);
                }
            }
            catch (IOException)
            {
                //TODO: log
            }
        }

        private ConcurrentDictionary<string, Tuple<DateTime, string>> DeserializeObject(Stream cacheStream)
        {
            try
            {
                var deserializedObject =
                    binarySerializer.Deserialize<ConcurrentDictionary<string, Tuple<DateTime, string>>>(cacheStream);
                return deserializedObject;
            }
            catch (Exception)
            {
                return new ConcurrentDictionary<string, Tuple<DateTime, string>>();
            }
        }
    }
}