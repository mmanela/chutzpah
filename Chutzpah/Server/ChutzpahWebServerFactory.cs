using Chutzpah.Server.Models;
using Nancy.Hosting.Self;
using System;
using System.Net;
using System.Net.Sockets;

namespace Chutzpah.Server
{
    public class ChutzpahWebServerFactory : IChutzpahWebServerFactory
    {
        public ChutzpahServerHost CreateServer(string rootPath, int defaultPort)
        {
            var hostConfiguration = new HostConfiguration
            {
                UrlReservations = new UrlReservations { CreateAutomatically = true }
            };

            var port = defaultPort;
            var host = new NancyHost(new Uri("http://localhost:1234"), new NancySettingsBootstrapper(rootPath), hostConfiguration);
            host.Start();
            return new ChutzpahServerHost(host, rootPath, port);
        }


        int GetNextAvailablePort(int port)
        {
            IPEndPoint endPoint;
            while (true)
            {
                try
                {
                    endPoint = new IPEndPoint(IPAddress.Any, port);
                    break;
                }
                catch (SocketException)
                {
                    ChutzpahTracer.TraceWarning("Unable to get port {0} so trying next one", port);
                    port++;
                }
            }

            return port;
        }
    }
}
