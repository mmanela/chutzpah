

using System.IO;

namespace Chutzpah.Wrappers
{
    public class JsonSerializer : IJsonSerializer
    {
        public T DeserializeFromFile<T>(string path)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromReader<T>(new StreamReader(path));
        }

        public T Deserialize<T>(string response)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(response);
        }

        public string Serialize<T>(T @object)
        {
            return ServiceStack.Text.JsonSerializer.SerializeToString<T>(@object);
        }
    }
}