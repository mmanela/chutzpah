namespace Chutzpah.Models
{
    public class BrowserTestFileResult
    {
        public BrowserTestFileResult(TestContext testContext, string browserOutput)
        {
            TestContext = testContext;
            BrowserOutput = browserOutput;
        }

        public TestContext TestContext { get; set; }
        public string BrowserOutput { get; set; }
    }
}