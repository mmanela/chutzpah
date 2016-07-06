using Chutzpah.Models;
using Chutzpah.Server.Models;
using Nancy.Hosting.Self;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Chutzpah.Server
{
    public class ChutzpahWebServerFactory : IChutzpahWebServerFactory
    {
        readonly IFileProbe fileProbe;

        public ChutzpahWebServerFactory(IFileProbe fileProbe)
        {
            this.fileProbe = fileProbe;
        }

        public ChutzpahWebServerHost CreateServer(ChutzpahWebServerConfiguration configuration)
        {
            if(ChutzpahWebServerHost.ActiveWebServer != null && ChutzpahWebServerHost.ActiveWebServer.RootPath.Equals(configuration.RootPath, StringComparison.OrdinalIgnoreCase))
            {
                // If the requested server is already running just re-use it
                return ChutzpahWebServerHost.ActiveWebServer;
            }

            var hostConfiguration = new HostConfiguration
            {
                RewriteLocalhost = true,
                UrlReservations = new UrlReservations { CreateAutomatically = true }
            };

            var port = GetNextAvailablePort(configuration.DefaultPort.Value);
            var builtInDependencyFolder = fileProbe.BuiltInDependencyDirectory;

            ChutzpahTracer.TraceInformation("Creating Web Server Host at path {0} and port {1}", configuration.RootPath, port);

            var host = new NancyHost(new Uri(string.Format("http://localhost:{0}", port)), new NancySettingsBootstrapper(configuration.RootPath, builtInDependencyFolder), hostConfiguration);
            host.Start();
            var chutzpahWebServerHost = ChutzpahWebServerHost.Create(host, configuration.RootPath, port);
            return chutzpahWebServerHost;
        }


        int GetNextAvailablePort(int port)
        {
            while (!IsPortOpen(port))
            {
                ChutzpahTracer.TraceWarning("Unable to get port {0} so trying next one", port);
                port++;

            }

            return port;
        }

        public static bool IsPortOpen(int port)
        {
            HttpListener listener = null;

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://*:{0}/", port));
                listener.Start();

                return true;
            }
            catch (HttpListenerException)
            {
            }
            catch(SocketException)
            {

            }
            finally
            {

                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                }
            }

            return false;
        }
    }
}
