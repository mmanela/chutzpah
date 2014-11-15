using Chutzpah.Wrappers;
using System.Collections.Generic;

namespace Chutzpah.Transformers
{
    public static class SummaryTransformerFactory
    {
        public static IEnumerable<SummaryTransformer> GetTransformers(IFileSystemWrapper fileSystem)
        {
            return new SummaryTransformer[] 
            {
                new JUnitXmlTransformer(fileSystem),
                new LcovTransformer(fileSystem)
            };
        }
    }
}