namespace Chutzpah.Models
{
    /// <summary>
    /// Describes different ways in which a test can be launched.
    /// </summary>
    public enum TestLaunchMode
    {
        /// <summary>
        /// Launch the test in a headless browser.
        /// </summary>
        HeadlessBrowser,
        
        /// <summary>
        /// Launch the test in a full web browser.
        /// </summary>
        FullBrowser,

        /// <summary>
        /// Launch the test via a/the ITestLauncher object.
        /// </summary>
        Custom,
    }
}