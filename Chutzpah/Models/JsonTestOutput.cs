using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class JsonTestOutput
    {
        public JsonTestOutput()
        {
            Results = new List<JsonTestCase>();
            Logs = new List<JsonLogOutput>();
            Errors = new List<string>();
        }

        public IList<string> Errors { get; set; }
        public IList<JsonLogOutput> Logs { get; set; }
        public IList<JsonTestCase> Results { get; set; }
    }
}