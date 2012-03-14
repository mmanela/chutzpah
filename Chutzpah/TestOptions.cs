namespace Chutzpah
{
    public class TestOptions
    {
        /// <summary>
        /// Whether or not to launch the tests in the defaul browser
        /// </summary>
        public bool OpenInBrowser { get; set; }

        /// <summary>
        /// The time to wait for the tests to compelte in milliseconds
        /// </summary>
        public int? TimeOutMilliseconds { get; set; }
    }
}