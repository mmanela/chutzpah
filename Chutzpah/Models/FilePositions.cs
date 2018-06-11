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

        public int TotalTestsCount()
        {
            return positions.Count();
        }

        public FilePosition this[int index]
        {
            get
            {
                return this.positions[index];
            }
        }

        public FilePosition this[string testName]
        {
            get
            {
                var matches = this.positions.Where(x => x.TestName.Equals(testName.Trim())).ToList();
                if (matches.Count == 1)
                {
                    return matches[0];
                }

                return null;
            }
        }


        public bool Contains(int index)
        {
            return index >= 0 && index < positions.Count;
        }

        public bool Contains(string testName)
        {
            return this.positions.Any(x => x.TestName.Equals(testName.Trim()));
        }

        public void Add(int line, int column, string testName)
        {
            this.positions.Add(new FilePosition(line, column, testName.Trim()));
        }
    }
}