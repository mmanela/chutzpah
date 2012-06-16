using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class JsLogs : JsRunnerOutput
    {
        public IEnumerable<TestLog> Logs { get; set; }
    }
}