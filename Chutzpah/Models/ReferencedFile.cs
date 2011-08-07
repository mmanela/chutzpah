using System.Collections.Generic;
using System.Collections;
namespace Chutzpah.Models
{
    public class ReferencedFile
    {
        public ReferencedFile()
        {
            FilePositions = new FilePositions();
        }

        public bool IsFileUnderTest { get; set; }
        public string StagedPath { get; set; }
        public string Path { get; set; }
        public bool IsLocal { get; set; }
        public FilePositions FilePositions { get; set; }
    }
}