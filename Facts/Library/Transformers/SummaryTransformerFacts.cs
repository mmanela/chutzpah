using Chutzpah.Transformers;
using System.Linq;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class SummaryTransformerFacts
    {
        [Fact]
        public void Supports_Lcov()
        {
            Assert.True(SummaryTransformerFactory.GetTransformers().Any(x => x.GetType() == typeof(LcovTransformer)));
        }

        [Fact]
        public void Supports_JUnitXmlTransformer()
        {
            Assert.True(SummaryTransformerFactory.GetTransformers().Any(x => x.GetType() == typeof(JUnitXmlTransformer)));
        }
    }
}
