using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class JsonLogOutput
    {
        public string Message { get; set; }
        public string Source { get; set; }
        public int Line { get; set; }
    }
}