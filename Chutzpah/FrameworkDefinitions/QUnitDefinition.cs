namespace Chutzpah.FrameworkDefinitions
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Definition that describes the QUnit framework.
    /// </summary>
    public class QUnitDefinition : BaseFrameworkDefinition
    {
        /// <summary>
        /// Gets a short, file system friendly key for the QUnit library.
        /// </summary>
        protected override string FrameworkKey
        {
            get
            {
                return "qunit";
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable QUnit file.
        /// </summary>
        protected override Regex FrameworkSignature
        {
            get
            {
                return RegexPatterns.QUnitTestRegex;
            }
        }
    }
}
