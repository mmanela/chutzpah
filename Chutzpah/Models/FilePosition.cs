using System.Collections.Generic;
using System.Collections;
namespace Chutzpah.Models
{
    public class FilePosition
    {
        public FilePosition()
        {
        }

        public FilePosition(int line, int column, string testName)
        {
            Line = line;
            Column = column;
            TestName = testName;
        }
        public int Line { get; set; }
        public int Column { get; set; }
        public string TestName { get; set; }
    }
}