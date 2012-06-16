using System.Collections.Generic;

namespace Chutzpah.Models.JS
{
    public class JsErrors : JsRunnerOutput
    {
        public IEnumerable<TestError> Errors { get; set; }
    }
}