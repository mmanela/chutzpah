namespace Chutzpah.Wrappers
{
    public interface IJsonSerializer
    {
        T Deserialize<T>(string json);
        string Serialize<T>(T @object);
        T DeserializeFromFile<T>(string path);
    }
}