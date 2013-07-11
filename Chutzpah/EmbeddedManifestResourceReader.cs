using System.IO;

namespace Chutzpah
{
    public static class EmbeddedManifestResourceReader
    {
        public static Stream GetEmbeddedResoureStream<T>(string path)
        {
            return typeof (T).Assembly.GetManifestResourceStream(typeof(T), path);
        }

        public static string GetEmbeddedResoureText<T>(string path)
        {
            using (var stream = GetEmbeddedResoureStream<T>(path))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}