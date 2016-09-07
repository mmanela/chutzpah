using Microsoft.AspNetCore.Hosting;
using System;

namespace Chutzpah.Server.Models
{
    public interface IChutzpahWebServerHost : IDisposable
    {
        string RootPath { get; set; }

        int Port { get; set; }

        bool IsRunning { get; set; }
    }

    public class ChutzpahWebServerHost : IChutzpahWebServerHost
    {
        public static ChutzpahWebServerHost Create(IWebHost webHost, string rootPath, int port)
        {
            var host = new ChutzpahWebServerHost(webHost, rootPath, port);
            return host;
        }

        public IWebHost WebHost { get; set; }
        public string RootPath { get; set; }

        public int Port { get; set; }

        public bool IsRunning { get; set; }

        private ChutzpahWebServerHost(IWebHost webHost, string rootPath, int port)
        {
            Port = port;
            RootPath = rootPath;
            WebHost = webHost;
            IsRunning = true;
        }

        public void Dispose()
        {
            try
            {
                ChutzpahTracer.TraceInformation("Tearing down Web Server Host at path {0} and port {1}", RootPath, Port);
                IsRunning = false;
                WebHost.Dispose();
            }
            catch (Exception e)
            {
                ChutzpahTracer.TraceError(e, "Error tearing down Web Server Host at path {0} and port {1}", RootPath, Port);
            }
        }

        public override string ToString()
        {
            return string.Format("ChutzpahServerHost - Port: {0}, RootPath: {1}", Port, RootPath);
        }
    }
}
