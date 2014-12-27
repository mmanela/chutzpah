using Chutzpah.Wrappers;
using SourceMapDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Coverage
{
    public class SourceMapDotNetLineCoverageMapper : ILineCoverageMapper
    {
        IFileSystemWrapper fileSystem;

        public SourceMapDotNetLineCoverageMapper(IFileSystemWrapper fileSystem) 
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            this.fileSystem = fileSystem;
        }

        public int?[] GetOriginalFileLineExecutionCounts(int?[] generatedSourceLineExecutionCounts, int sourceLineCount, string mapFilePath)
        {
            if (generatedSourceLineExecutionCounts == null)
            {
                throw new ArgumentNullException("generatedSourceLineExecutionCounts");
            }
            else if (string.IsNullOrWhiteSpace(mapFilePath)) 
            {
                return generatedSourceLineExecutionCounts;
            }
            else if (!fileSystem.FileExists(mapFilePath)) 
            {
                throw new ArgumentException("mapFilePath", string.Format("Cannot find map file '{0}'", mapFilePath));
            }

            var consumer = this.GetConsumer(fileSystem.GetText(mapFilePath));

            var accumulated = new List<int?>(new int?[sourceLineCount + 1]);
            for (var i = 1; i < generatedSourceLineExecutionCounts.Length; i++)
            {
                int? generatedCount = generatedSourceLineExecutionCounts[i];
                if (generatedCount == null)
                {
                    continue;
                }

                var matches = consumer.OriginalPositionsFor(i);
                if (matches.Any())
                {
                    foreach (var match in matches)
                    {
                        accumulated[match.LineNumber] = (accumulated[match.LineNumber] ?? 0) + generatedCount.Value;
                    }
                }
            }

            return accumulated.ToArray();
        }

        protected virtual ISourceMapConsumer GetConsumer(string mapFileContents)
        {
            return new SourceMapConsumer(mapFileContents);
        }
    }
}
