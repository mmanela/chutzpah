using Chutzpah.Models;
using System;
using System.Globalization;
using System.IO;
using System.Web;

namespace Chutzpah
{
    public class UrlBuilder : IUrlBuilder
    {
        readonly IFileProbe fileProbe;

        public UrlBuilder(IFileProbe fileProbe)
        {
            this.fileProbe = fileProbe;
        }

        private static string EncodeFilePath(string path)
        {
            return HttpUtility.UrlEncode(path)
                .Replace("+", "%20")
                .Replace("%3a", ":")
                .Replace("%5c", "/")
                .Replace("%2f", "/");
        }

        public string GenerateFileUrl(TestContext testContext, ReferencedFile referencedFile)
        {
            var path = referencedFile.GeneratedFilePath ?? referencedFile.Path;
            return GenerateFileUrl(testContext, path, fullyQualified: false, isBuiltInDependency: referencedFile.IsBuiltInDependency, fileHash: referencedFile.Hash);
        }

        public string GenerateFileUrl(TestContext testContext, string absolutePath, bool fullyQualified = false, bool isBuiltInDependency = false, string fileHash = null)
        {
            var isRunningInWebServer = testContext.TestFileSettings.Server != null && testContext.TestFileSettings.Server.Enabled.GetValueOrDefault();

            if (!RegexPatterns.SchemePrefixRegex.IsMatch(absolutePath))
            {
                if (isRunningInWebServer)
                {
                    return GenerateServerFileUrl(testContext, absolutePath, fullyQualified, isBuiltInDependency, fileHash);
                }
                else
                {
                    return GenerateLocalFileUrl(absolutePath);
                }
            }

            return absolutePath;
        }


        /// <summary>
        /// This generates a file url based on an absolute file path
        /// </summary>
        public string GenerateLocalFileUrl(string absolutePath)
        {
            var encodedReferencePath = EncodeFilePath(absolutePath);
            var fileUrlFormat = encodedReferencePath.StartsWith("//", StringComparison.OrdinalIgnoreCase) ? "file://{0}" : "file:///{0}";
            return string.Format(fileUrlFormat, encodedReferencePath);
        }

        public string GenerateAbsoluteServerUrl(TestContext testContext, ReferencedFile referencedFile)
        {
            var isRunningInWebServer = testContext.TestFileSettings.Server != null && testContext.TestFileSettings.Server.Enabled.GetValueOrDefault();
            if(!isRunningInWebServer)
            {
                return null;
            }

            var path = referencedFile.GeneratedFilePath ?? referencedFile.Path;
            if (!RegexPatterns.SchemePrefixRegex.IsMatch(path))
            {
                return GenerateServerFileUrl(testContext, path, fullyQualified:true, isBuiltInDependency: referencedFile.IsBuiltInDependency, fileHash: referencedFile.Hash);
            }

            return path;
        }


        /// <summary>
        /// Generate a file url that can be used when hosting it on a server
        /// </summary>
        public string GenerateServerFileUrl(TestContext testContext, string absolutePath, bool fullyQualified, bool isBuiltInDependency, string fileHash)
        {
            var port = testContext.WebServerHost.Port;
            var rootPath = testContext.WebServerHost.RootPath;

            string relativePath = null;
            string parentPath = null;
            if (isBuiltInDependency)
            {
                // We need a fully qualified url when using vitural server path
                fullyQualified = true;

                parentPath = fileProbe.BuiltInDependencyDirectory;

                // Pass false to GetRelativePath to leave uri encoded since we want that when hosting on a server
                relativePath = NormalizeUrlPath(GetRelativePath(parentPath, absolutePath, false));

                relativePath = string.Format("{0}/{1}", Constants.ServerVirtualBuiltInFilesPath, relativePath);
            }
            else
            {
                // If we are fully qualified we generate relative to the root path of the webserver
                // otherwise generate relative to test harness
                parentPath = fullyQualified ? rootPath : testContext.TestHarnessDirectory;


                // Pass false to GetRelativePath to leave uri encoded since we want that when hosting on a server
                relativePath = NormalizeUrlPath(GetRelativePath(parentPath, absolutePath, false));
            }

            var url = fullyQualified ? string.Format("http://localhost:{0}/{1}", port, relativePath) : relativePath;

            return string.IsNullOrEmpty(fileHash) ? url : $"{url}?{Constants.FileUrlShaKey}={fileHash}";
        }


        public static string NormalizeFilePath(string path)
        {
            if (path == null) return null;

            return path.ToLowerInvariant().Replace(@"/", @"\");
        }

        public static string NormalizeUrlPath(string path)
        {
            if (path == null) return null;

            return path.Replace(@"\", @"/");
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