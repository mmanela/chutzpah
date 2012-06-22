

namespace Chutzpah.Wrappers
{
    public class JsonSerializer : IJsonSerializer
    {
        public T Deserialize<T>(string response)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(response);
        }
    }
}