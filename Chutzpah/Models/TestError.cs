using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Chutzpah.Models
{
    public class TestError
    {
        public TestError()
        {
            Stack = new List<Stack>();
        }

        public string InputTestFile { get; set; }
        public string Message { get; set; }
        public IList<Stack> Stack { get; set; }
        public string StackAsString { get; set; }
        public string PathFromTestSettingsDirectory { get; internal set; }

        public string GetFormattedStackTrace()
        {
            if (!string.IsNullOrEmpty(StackAsString))
            {
                return string.Join("\n", Regex.Split(StackAsString, "\r?\n").Select(s => "\t" + s)) + "\n";
            }
            else if (Stack != null)
            {
                return FormatStackObject();
            }

            return "";
        }

        public string FormatStackObject()
        {
            if (Stack != null)
            {
                var stack = "";
                foreach (var item in Stack)
                {
                    if (!string.IsNullOrEmpty(item.Function))
                    {
                        stack += "at " + item.Function + " ";
                    }
                    if (!string.IsNullOrEmpty(item.File))
                    {
                        stack += "in " + item.File;
                    }
                    if (!string.IsNullOrEmpty(item.Line))
                    {
                        stack += string.Format(" (line {0})", item.Line);
                    }
                    stack += "\n";
                }
                return stack;
            }
            else
            {
                return "";
            }

        }
    }
}