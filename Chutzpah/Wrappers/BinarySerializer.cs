using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Chutzpah.Wrappers
{
    public interface IBinarySerializer
    {
        T Deserialize<T>(Stream stream);
        void Serialize(Stream stream, object @object);
    }

    public class BinarySerializer : IBinarySerializer
    {
        public T Deserialize<T>(Stream stream)
        {
            var binaryFormatter = new BinaryFormatter();
            return (T)binaryFormatter.Deserialize(stream);
        }

        public void Serialize(Stream stream, object @object)
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, @object);

        }
    }
}