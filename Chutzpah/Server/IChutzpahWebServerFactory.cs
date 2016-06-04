using Chutzpah.Server.Models;

namespace Chutzpah.Server
{
    public interface IChutzpahWebServerFactory
    {
        ChutzpahServerHost CreateServer(string rootPath, int defaultPort);
    }
}