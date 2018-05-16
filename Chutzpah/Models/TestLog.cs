namespace Chutzpah.Models
{
    public class TestLog
    {
        public string InputTestFile { get; set; }
        public string Message { get; set; }
        public string PathFromTestSettingsDirectory { get; internal set; }
    }
}