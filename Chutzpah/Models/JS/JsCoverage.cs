using System.Collections.Generic;

namespace Chutzpah.Models.JS
{
    public class JsCoverage : JsRunnerOutput
    {
        /// <summary>
        /// Maps each individual script with its full path to an object that contains
        /// coverage information from the execution of that script.
        /// </summary>
        //public IDictionary<string, ScriptCoverage> Object { get; set; }
        public CoverageData Object { get; set; }
    }

    public class CoverageData : Dictionary<string, ScriptCoverage>
    {
    }

    /// <summary>
    /// Contains coverage information for a single script file.
    /// </summary>
    public class ScriptCoverage
    {
        /// <summary>
        /// For each line in the source code, contains <c>null</c> if the line is irrelevant, or
        /// an integer value representing the number of times the line was executed. Line numbers
        /// are 1-based, so the first array item is always <c>null</c>.
        /// </summary>
        public int?[] Coverage { get; set; }

        /// <summary>
        /// Contains all lines in the script source code. Lines are 0-based, so the first array
        /// item contains the first source line.
        /// </summary>
        public string[] Source { get; set; }

        /// <summary>
        /// For each line in the source code, contains <c>null</c> if the line doesn't contain
        /// a condition, or an array of conditions on that line. Conditions are 1-based, so the
        /// first item in a line array is always <c>null</c>.
        /// </summary>
        public BranchCondition[][] BranchData { get; set; }
    }

    /// <summary>
    /// Represents a single JavaScript condition.
    /// </summary>
    public class BranchCondition
    {
        /// <summary>
        /// The position of the condition. Unclear what this value is - it doesn't match
        /// character position.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The length of the condition in the source code.
        /// </summary>
        public int NodeLength { get; set; }

        /// <summary>
        /// A string representation of the condition, as found in the source code.
        /// </summary>
        public string Src { get; set; }

        /// <summary>
        /// The number of times the condition was evaluated to false.
        /// </summary>
        public int EvalFalse { get; set; }

        /// <summary>
        /// The number of times the condition was evaluated to true.
        /// </summary>
        public int EvalTrue { get; set; }
    }
}
