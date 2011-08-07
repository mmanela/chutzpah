using System.Collections.Generic;
using System.Collections;
namespace Chutzpah.Models
{
    public class FilePosition
    {
        public FilePosition()
        {
        }

        public FilePosition(int line, int column)
        {
            Line = line;
            Column = column;
        }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}