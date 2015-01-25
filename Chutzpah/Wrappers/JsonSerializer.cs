using System.IO;
using Newtonsoft.Json;

namespace Chutzpah.Wrappers
{
    public class JsonSerializer : IJsonSerializer
    {
        public T DeserializeFromFile<T>(string path)
        {   
            // NOTE: This method is use Json.Net instead of the servicestack serializer.
            //       The reason is Json.Net is a more friendly parser for a user facing thing like the 
            //       settings file but the servicestack is much faster for use in communication with phantom
            var text = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(text);
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