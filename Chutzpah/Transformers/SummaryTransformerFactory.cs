using System.Collections.Generic;

namespace Chutzpah.Transformers
{
    public static class SummaryTransformerFactory
    {
        public static IEnumerable<SummaryTransformer> GetTransformers()
        {
            return new SummaryTransformer[] {
                             new JUnitXmlTransformer(),
                             new LcovTransformer()
                         };
        }
    }
}