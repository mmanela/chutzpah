using Chutzpah.Transformers;
using Chutzpah.Wrappers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Chutzpah.Facts.Library.Transformers
{
    public class SummaryTransformerProviderFacts
    {
        private IFileSystemWrapper GetFileSystemWrapper()
        {
            return new Mock<IFileSystemWrapper>().Object;
        }

        [Fact]
        public void ReturnsSummaryTransformers_AsPerSummaryTransformerFactory()
        {
            var expected = SummaryTransformerFactory.GetTransformers(GetFileSystemWrapper());
            var actual = new SummaryTransformerProvider().GetTransformers(GetFileSystemWrapper());

            foreach (var actualTransformer in actual)
            {
                var matchingByType = expected.FirstOrDefault(x => x.GetType() == actualTransformer.GetType());

                Assert.NotNull(matchingByType);
                Assert.Equal(matchingByType.Name, actualTransformer.Name);
                Assert.Equal(matchingByType.Description, actualTransformer.Description);
            }

            Assert.Equal(expected.Count(), actual.Count());
        }
    }
}
