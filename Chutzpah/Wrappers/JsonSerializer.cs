using Newtonsoft.Json;

namespace Chutzpah.Wrappers
{
    public class JsonSerializer : IJsonSerializer
    {
        public T Deserialize<T>(string response)
        {
            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}