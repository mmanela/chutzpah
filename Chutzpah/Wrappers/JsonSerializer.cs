

namespace Chutzpah.Wrappers
{
    public class JsonSerializer : IJsonSerializer
    {
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