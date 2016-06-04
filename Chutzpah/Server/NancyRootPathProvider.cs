using Nancy;

namespace Chutzpah.Server
{
    class NancyRootPathProvider : IRootPathProvider
    {
        public readonly string RootPath;
        public NancyRootPathProvider(string rootPath)
        {
            RootPath = rootPath;
        }

        public string GetRootPath()
        {
            return RootPath;
        }
    }
}
