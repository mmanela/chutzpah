using System.Web;

namespace Chutzpah.Wrappers
{
    public class HtmlUtility : IHtmlUtility
    {
        public string DecodeJavaScript(string text)
        {
            return HttpUtility.HtmlDecode(HttpUtility.UrlDecode(text));
        }
    }
}