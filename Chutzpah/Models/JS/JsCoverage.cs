namespace Chutzpah.Models.JS
{
    public class JsCoverage : JsRunnerOutput
    {
        /// <summary>
        /// String representation of a coverage object. This is parsed into a full-fledged
        /// typed object by a coverage engine.
        /// </summary>
        public string Object { get; set; }
    }
}
