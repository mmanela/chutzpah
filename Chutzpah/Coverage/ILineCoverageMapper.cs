using Chutzpah.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Coverage
{
    public interface ILineCoverageMapper
    {
        int?[] GetOriginalFileLineExecutionCounts(int?[] generatedSourceLineExecutionCounts, int sourceLineCount, string mapFile);
    }
}
