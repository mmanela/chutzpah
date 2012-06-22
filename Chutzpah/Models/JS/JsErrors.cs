using System.Collections.Generic;

namespace Chutzpah.Models.JS
{
    public class JsError : JsRunnerOutput
    {
        public TestError Error { get; set; }
    }
}