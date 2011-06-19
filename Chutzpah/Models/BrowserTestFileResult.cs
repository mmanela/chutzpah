namespace Chutzpah.Models
{
    public class BrowserTestFileResult
    {
        public BrowserTestFileResult(string htmlTestFile, string inputTestFile, string browserOutput)
        {
            HtmlTestFile = htmlTestFile;
            InputTestFile = inputTestFile;
            BrowserOutput = browserOutput;
        }

        public string HtmlTestFile { get; set; }
        public string InputTestFile { get; set; }
        public string BrowserOutput { get; set; }
    }
}