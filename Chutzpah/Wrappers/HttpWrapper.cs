using System.Net;

namespace Chutzpah.Wrappers
{
    public interface IHttpWrapper
    {
        string GetContent(string url);
    }

    public class HttpWrapper : IHttpWrapper
    {
        public string GetContent(string url)
        {
            var webClient = new WebClient();
            return webClient.DownloadString(url);
        }    
    }
}