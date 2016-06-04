using Nancy.Hosting.Self;
using System;

namespace Chutzpah.Server.Models
{
    public class ChutzpahServerHost : IDisposable
    {
        public NancyHost NancyHost { get; set; }
        public string RootPath { get; set; }

        public int Port { get; set; }

        public ChutzpahServerHost(NancyHost nancyHost, string rootPath, int port)
        {
            Port = port;
            RootPath = rootPath;
            NancyHost = nancyHost;
        }

        public void Dispose()
        {
            NancyHost.Dispose();
        }
    }
}
