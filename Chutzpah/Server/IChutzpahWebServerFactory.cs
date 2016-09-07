using Chutzpah.Models;
using Chutzpah.Server.Models;

namespace Chutzpah.Server
{
    public interface IChutzpahWebServerFactory
    {
        IChutzpahWebServerHost CreateServer(ChutzpahWebServerConfiguration configuration, IChutzpahWebServerHost activeWebServerHost);
    }
}