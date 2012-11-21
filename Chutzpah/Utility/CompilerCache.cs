using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Chutzpah.Utility
{
    class CompilerCache
    {
        private readonly Dictionary<string, Tuple<DateTime, string>> _compilerCache;
        private readonly string _fileName;
        private readonly int _maxSizeBytes;
        private readonly Hasher _hasher;
        


        public CompilerCache(string fileName, int maxSizeMb = 1)
        {
            _fileName = fileName;
            _maxSizeBytes = maxSizeMb * 1024 * 1024;
            _hasher = new Hasher();
            _compilerCache = File.Exists(_fileName) ? DeSerializeObject(_fileName) : new Dictionary<string, Tuple<DateTime, string>>();
        }

        public void Save()
        {
            limitSize();
            SerializeObject(_fileName, _compilerCache);
        }

        private void limitSize()
        {
            var size = _compilerCache.Sum(tuple => tuple.Value.Item2.Length);
            while (size > _maxSizeBytes)
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
            var objectToSerialize = (Dictionary<string, Tuple<DateTime, string>>)bFormatter.Deserialize(stream);
            stream.Close();
            return objectToSerialize;
        }

        public string Get(string coffeScriptSource)
        {
            var hash = _hasher.Hash(coffeScriptSource);
            return _compilerCache.ContainsKey(hash) ? _compilerCache[hash].Item2 : null;
        }

        public void Set(string source, string compiled)
        {
            var hash = _hasher.Hash(source);
            _compilerCache[hash] = new Tuple<DateTime, string>(DateTime.Now, compiled);
        }

        public void SetInProgress(string coffeScriptSource)
        {
            Set(coffeScriptSource,"INPROGRESS");
        }
    }
}
