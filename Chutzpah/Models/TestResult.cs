using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Chutzpah.Models
{
    public class TestResult
    {
        public bool Passed { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// For a failed test result, may contain the line number of the expectation, if known.
        /// Otherwise, contains the value <c>0</c>.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// For a test that failed with an exception, contains the stack trace, if known.
        /// </summary>
        public string StackTrace { get; set; }

        public string GetFailureMessage()
        {
            if (Passed) return "";
            var errorString = "";
            if (!string.IsNullOrWhiteSpace(Message))
            {
                errorString = Message;
            }
            else if (Expected != null || Actual != null)
            {
                errorString = string.Format("Expected: {0}, Actual: {1}", Expected, Actual);
            }
            else
            {
                errorString = "Assert failed";
            }

            if (LineNumber != 0)
            {
                errorString += string.Format(" (on line {0})", LineNumber);
            }

            if (StackTrace != null)
            {
                errorString += "\n" + string.Join("\n", Regex.Split(StackTrace, "\r?\n").Select(s => "\t" + s));
            }

            return errorString;
        }
    }
}