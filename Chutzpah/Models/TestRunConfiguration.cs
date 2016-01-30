using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Chutzpah.Models
{
    public class TestRunConfiguration
    {
        public List<List<PathInfo>> TestGroups { get; set; }

        public int? MaxDegreeOfParallelism { get; set; }

        public bool EnableTracing { get; set; }
        public string TraceFilePath { get; set; }

        public TestRunConfiguration()
        {
            TestGroups = new List<List<PathInfo>>();
            TraceFilePath = Path.Combine(Path.GetTempPath(), Constants.LogFileName);
        }

    }
}
