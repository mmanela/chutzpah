using System.Collections.Generic;
using System.Collections;
namespace Chutzpah.Models
{
    public class FilePositions
    {
        private readonly Dictionary<string, FilePosition> mapping;
        const string TestNameFormat = "{0}::{1}";

        public FilePositions()
        {
            mapping = new Dictionary<string, FilePosition>();
        }

        public FilePosition Get(string moduleName, string testName)
        {
            var key = string.Format(TestNameFormat, moduleName, testName);
            if (mapping.ContainsKey(key))
            {
                return mapping[key];
            }

            return new FilePosition();
        }

        public void Add(string moduleName, string testName, int line, int column)
        {
            moduleName = moduleName ?? "";
            mapping[string.Format(TestNameFormat, moduleName, testName)] = new FilePosition(line, column);
        }
    }
}