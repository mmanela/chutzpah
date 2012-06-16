namespace Chutzpah.Models
{
    public class JsFileDone : JsRunnerOutput
    {
        public int TimeTaken { get; set; }
        public int Failed { get; set; }
        public int Passed { get; set; }
    }
}