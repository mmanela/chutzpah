using Chutzpah.Models;
using Chutzpah.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Nancy.Hosting.Self;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Primitives;

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
            if (ChutzpahWebServerHost.ActiveWebServer != null && ChutzpahWebServerHost.ActiveWebServer.RootPath.Equals(configuration.RootPath, StringComparison.OrdinalIgnoreCase))
            {
                // If the requested server is already running just re-use it
                return ChutzpahWebServerHost.ActiveWebServer;
            }

            var builtInDependencyFolder = fileProbe.BuiltInDependencyDirectory;


            return BuildHost(configuration.RootPath, configuration.DefaultPort.Value, builtInDependencyFolder);
        }

        private ChutzpahWebServerHost BuildHost(string rootPath, int defaultPort, string builtInDependencyFolder)
        {
            var port = FindFreePort(defaultPort);

            ChutzpahTracer.TraceInformation("Creating Web Server Host at path {0} and port {1}", rootPath, port);
            var host = new WebHostBuilder()
               .UseUrls($"http://localhost:{port}")
               .UseContentRoot(rootPath)
               .UseWebRoot("")
               .UseKestrel()
               .Configure((app) =>
               {
                   var env = (IHostingEnvironment)app.ApplicationServices.GetService(typeof(IHostingEnvironment));
                   app.UseStaticFiles(new StaticFileOptions { FileProvider = new ChutzpahServerFileProvider(env.ContentRootPath, builtInDependencyFolder) });
                   app.Run(async (context) =>
                   {
                       await context.Response.WriteAsync("Chutzpah Web Server");
                   });
               })
               .Build();

            host.Run();

            return ChutzpahWebServerHost.Create(host, rootPath, port);
        }




        public static int FindFreePort(int initialPort)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, initialPort));
                }
                catch (SocketException)
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                }

                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

    }
}
