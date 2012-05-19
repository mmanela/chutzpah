namespace Chutzpah
{
    public class TestOptions
    {
        public TestOptions()
        {
            FileSearchLimit = 200;
        }

        /// <summary>
        /// Whether or not to launch the tests in the defaul browser
        /// </summary>
        public bool OpenInBrowser { get; set; }

        /// <summary>
        /// The time to wait for the tests to compelte in milliseconds
        /// </summary>
        public int? TimeOutMilliseconds { get; set; }

        /// <summary>
        /// This is the max number of files 
        /// </summary>
        public int FileSearchLimit { get; set; }
    }
}