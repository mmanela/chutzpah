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
        /// Launch the test in Internet Explorer and attach the Visual Studio debugger 
        /// to it (using the Script debug engine).
        /// </summary>
        ScriptDebugger,
    }
}