using Chutzpah.Transformers;
using Chutzpah.Wrappers;
using Moq;
using System.Linq;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class SummaryTransformerFacts
    {
        private IFileSystemWrapper GetFileSystemWrapper()
        {
            return new Mock<IFileSystemWrapper>().Object;
        }

        [Fact]
        public void Supports_Lcov()
        {
            Assert.True(SummaryTransformerFactory.GetTransformers(GetFileSystemWrapper()).Any(x => x.GetType() == typeof(LcovTransformer)));
        }

        [Fact]
        public void Supports_JUnitXmlTransformer()
        {
            Assert.True(SummaryTransformerFactory.GetTransformers(GetFileSystemWrapper()).Any(x => x.GetType() == typeof(JUnitXmlTransformer)));
        }
    }
}
