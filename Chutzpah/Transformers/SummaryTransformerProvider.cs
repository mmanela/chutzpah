using Chutzpah.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Transformers
{
    public class SummaryTransformerProvider : ISummaryTransformerProvider
    {
        public IEnumerable<SummaryTransformer> GetTransformers(IFileSystemWrapper fileSystem)
        {
            return SummaryTransformerFactory.GetTransformers(fileSystem);
        }
    }
}
