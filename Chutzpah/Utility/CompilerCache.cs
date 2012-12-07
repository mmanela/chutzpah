using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Chutzpah.Utility
{
    class CompilerCache : ICompilerCache
    {
        private readonly Dictionary<string, Tuple<DateTime, string>> _compilerCache;
        private readonly Hasher _hasher;
        private readonly GlobalOptions _globalOptions;

        public CompilerCache()
        {
            _globalOptions = GlobalOptions.Instance;
            _hasher = new Hasher();
            _compilerCache = File.Exists(_globalOptions.CompilerCacheFile)
                                    ? DeSerializeObject(_globalOptions.CompilerCacheFile)
                                    : new Dictionary<string, Tuple<DateTime, string>>();

            
        }

        ~CompilerCache()
        {
            Save();
        }

        private void LimitSize()
        {
            var size = _compilerCache.Sum(tuple => tuple.Value.Item2.Length);
            while (size > _globalOptions.CompilerCacheMaxSize*1024*1024)
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
            Stream stream = File.Open(filename, FileMode.Create);
            var bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, objectToSerialize);
            stream.Close();
        }

        private Dictionary<string, Tuple<DateTime, string>> DeSerializeObject(string filename)
        {
            Stream stream = File.Open(filename, FileMode.Open);
            var bFormatter = new BinaryFormatter();
            var deserializedObject = (Dictionary<string, Tuple<DateTime, string>>)bFormatter.Deserialize(stream);
            stream.Close();
            return deserializedObject;
        }

        public string Get(string source)
        {
            if (_globalOptions.EnableCompilerCache)
            {
                var hash = _hasher.Hash(source);
                return _compilerCache.ContainsKey(hash) ? _compilerCache[hash].Item2 : null;
            }
            return null;
        }


        public void Set(string source, string compiled)
        {
            if (!_globalOptions.EnableCompilerCache) return;
            var hash = _hasher.Hash(source);
            _compilerCache[hash] = new Tuple<DateTime, string>(DateTime.Now, compiled);
        }

        public void Save()
        {
            LimitSize();
            SerializeObject(_globalOptions.CompilerCacheFile, _compilerCache);
        }

    }
}
