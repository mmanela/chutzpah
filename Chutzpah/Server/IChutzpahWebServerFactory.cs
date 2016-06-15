using Chutzpah.Models;
using Chutzpah.Server.Models;

namespace Chutzpah.Server
{
    public interface IChutzpahWebServerFactory
    {
        ChutzpahWebServerHost CreateServer(ChutzpahWebServerConfiguration configuration);
    }
}