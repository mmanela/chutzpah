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
        private string _filename;
        


        public CompilerCache(IFileSystemWrapper fileSystem)
        {
            
            _hasher = new Hasher();
            _compilerCache = new Dictionary<string, Tuple<DateTime, string>>();
            _filesystem = fileSystem;
            if (string.IsNullOrEmpty(GlobalOptions.Instance.CompilerCacheFile))
            {
                _filename = Path.Combine(_filesystem.GetTemporaryFolder(Constants.ChutzpahCompilerCacheFolder),
                                         Constants.ChutzpahCompilerCacheFileName);
            }
            else
            {
                _filename = GlobalOptions.Instance.CompilerCacheFile;
            }

            if (fileSystem.FileExists(_filename))
            {
                using (var cacheStream = fileSystem.Open(_filename, FileMode.Open, FileAccess.Read))
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
            var size = _compilerCache.Sum(tuple => tuple.Value.Item2.Length + tuple.Value.Item1.ToString().Length + tuple.Key.Length*2);
            while (size > Constants.CompilerCacheFileMaxSize)
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
            LimitSize();
            SerializeObject( _filename, _compilerCache);
        }

    }
}
