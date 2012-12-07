using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Chutzpah.Wrappers;

namespace Chutzpah.Utility
{
    public class CompilerCache : ICompilerCache
    {
        private readonly Dictionary<string, Tuple<DateTime, string>> _compilerCache;
        private readonly Hasher _hasher;
        private IFileSystemWrapper _filesystem;


        public CompilerCache(IFileSystemWrapper fileSystem)
        {
            
            _hasher = new Hasher();
            _compilerCache = new Dictionary<string, Tuple<DateTime, string>>();
            _filesystem = fileSystem;
            if (!string.IsNullOrEmpty( GlobalOptions.Instance.CompilerCacheFile) &&
                fileSystem.FileExists( GlobalOptions.Instance.CompilerCacheFile))
            {
                using (var cacheStream = fileSystem.Open( GlobalOptions.Instance.CompilerCacheFile, FileMode.Open, FileAccess.Read))
                {
                    _compilerCache = DeSerializeObject(cacheStream);
                }
            }
            
            

            
        }

        ~CompilerCache()
        {
            Save();
        }

        private void LimitSize()
        {
            var size = _compilerCache.Sum(tuple => tuple.Value.Item2.Length + tuple.Value.Item1.ToString().Length + tuple.Key.Length);
            while (size >  GlobalOptions.Instance.CompilerCacheMaxSize*1024*1024)
            {
                var oldestKey = "";
                var oldestTime = DateTime.Now;
                foreach (var tuple in _compilerCache)
                {
                    if (tuple.Value.Item1 < oldestTime)
                    {
                        oldestTime = tuple.Value.Item1;
                        oldestKey = tuple.Key;
                    }
                }
                size -= _compilerCache[oldestKey].Item2.Length;
                _compilerCache.Remove(oldestKey);

            }
        }

        private void SerializeObject(string filename, Dictionary<string, Tuple<DateTime, string>> objectToSerialize)
        {
            using (var stream = _filesystem.Open(filename, FileMode.Create, FileAccess.Write))
            {
                var bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, objectToSerialize);
            }
        }

        private Dictionary<string, Tuple<DateTime, string>> DeSerializeObject(Stream cacheStream)
        {
            try
            {
                var bFormatter = new BinaryFormatter();
                var deserializedObject = (Dictionary<string, Tuple<DateTime, string>>)bFormatter.Deserialize(cacheStream);
                return deserializedObject;
            }
            catch (Exception)
            {
                // File was not a saved cache. Set filename to null to prevent it from being overwritten when saving.
                GlobalOptions.Instance.CompilerCacheFile = null;
                return new Dictionary<string, Tuple<DateTime, string>>();
            }
           
        }

        public string Get(string source)
        {
            var hash = _hasher.Hash(source);
            return _compilerCache.ContainsKey(hash) ? _compilerCache[hash].Item2 : null;
        }


        public void Set(string source, string compiled)
        {
            var hash = _hasher.Hash(source);
            _compilerCache[hash] = new Tuple<DateTime, string>(DateTime.Now, compiled);
        }

        public void Save()
        {
            if (!string.IsNullOrEmpty( GlobalOptions.Instance.CompilerCacheFile))
            {
                LimitSize();
                SerializeObject( GlobalOptions.Instance.CompilerCacheFile, _compilerCache);
            }
        }

    }
}
