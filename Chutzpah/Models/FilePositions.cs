using System.Linq;
using System.Collections.Generic;

namespace Chutzpah.Models
{

    public class FilePositions
    {
        private readonly List<FilePosition> positions;

        public FilePositions()
        {
            this.positions = new List<FilePosition>();
        }

        public FilePosition this[int index]
        {
            get
            {
                return this.positions[index];
            }
        }


        public bool Contains(int index)
        {
            return index >= 0 && index < positions.Count;
        }

        public bool Contains(string testName)
        {
            return this.positions.Any(x => x.TestName.Equals(testName));
        }

        public void Add(int line, int column, string testName)
        {
            this.positions.Add(new FilePosition(line, column, testName));
        }
    }
}