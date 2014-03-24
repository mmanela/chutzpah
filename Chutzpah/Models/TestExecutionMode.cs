namespace Chutzpah.Models
{
    /// <summary>
    /// Determines what operation the test runner should perform
    /// </summary>
    public enum TestExecutionMode
    {
        /// <summary>
        /// Run the tests and capture their results
        /// </summary>
        Execution,
        
        /// <summary>
        /// Discover what tests are in the file but do not run them
        /// </summary>
        Discovery
    }
}