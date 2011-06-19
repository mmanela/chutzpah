using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class JsonTestOutput
    {
        public JsonTestOutput()
        {
            Results = new List<JsonTestCase>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }

        public IList<string> Warnings { get; set; }
        public IList<string> Errors { get; set; }
        public IList<JsonTestCase> Results { get; set; }
    }
}