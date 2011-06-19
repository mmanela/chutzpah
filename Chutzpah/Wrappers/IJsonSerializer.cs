namespace Chutzpah.Wrappers
{
    public interface IJsonSerializer
    {
        T Deserialize<T>(string json);
        object Deserialize(string json);
    }
}