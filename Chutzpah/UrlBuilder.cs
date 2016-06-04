using System;
using System.Globalization;
using System.IO;
using System.Web;

namespace Chutzpah
{
    public class UrlBuilder
    {
        private static string EncodeFilePath(string path)
        {
            return HttpUtility.UrlEncode(path)
                .Replace("+", "%20")
                .Replace("%3a", ":")
                .Replace("%5c", "/")
                .Replace("%2f", "/");
        }

        /// <summary>
        /// This generates a file url based on an absolute file path
        /// </summary>
        public static string GenerateLocalFileUrl(string absolutePath)
        {
            var encodedReferencePath = EncodeFilePath(absolutePath);
            var fileUrlFormat = encodedReferencePath.StartsWith("//", StringComparison.OrdinalIgnoreCase) ? "file://{0}" : "file:///{0}";
            return string.Format(fileUrlFormat, encodedReferencePath);
        }

        /// <summary>
        /// Generate a file url that can be used when hosting it on a server
        /// </summary>
        public static string GenerateServerFileUrl(string rootPath, string absolutePath)
        {
            // Pass false to GetRelativePath to leave uri encoded since we want that when hosting on a server
            return GetRelativePath(rootPath, absolutePath, false).Replace("\\", "/");
        }
        

        public static string NormalizeFilePath(string path)
        {
            if (path == null) return null;

            return path.ToLowerInvariant().Replace(@"/", @"\");
        }

        /// <summary>
        /// This get a relative path from one path to another. 
        /// </summary>
        /// <param name="pathToStartFrom"></param>
        /// <param name="pathToGetTo"></param>
        /// <returns></returns>
        public static string GetRelativePath(string pathToStartFrom, string pathToGetTo, bool unescapeDateString = true)
        {
            var pathToGetToUri = new Uri(pathToGetTo);
            
            // Folders must end in a slash
            if (!pathToStartFrom.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
            {
                pathToStartFrom += Path.DirectorySeparatorChar;
            }
            
            var pathToStartFromUri = new Uri(pathToStartFrom);
            var path = pathToStartFromUri.MakeRelativeUri(pathToGetToUri).ToString().Replace('/', Path.DirectorySeparatorChar);

            return unescapeDateString ? Uri.UnescapeDataString(path) : path;
        }
    }
}