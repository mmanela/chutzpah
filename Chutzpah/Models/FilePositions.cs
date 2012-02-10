namespace Chutzpah.Models
{
    using System.Collections.Generic;

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

        public void Add(int line, int column)
        {
            this.positions.Add(new FilePosition(line, column));
        }
    }
}