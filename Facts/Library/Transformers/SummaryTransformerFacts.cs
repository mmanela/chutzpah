using System;
using System.Collections.Generic;
using Chutzpah.Models;
using Chutzpah.Transformers;
using Xunit;
using Chutzpah.Wrappers;
using Moq;
using System.Text;

namespace Chutzpah.Facts.Library.Transformers
{
    public class SummaryTransformerFacts
    {
        [Fact]
        public void Will_write_utf8_without_byte_order_mark()
        {
            Encoding summaryEncoding = null;

            var fsw = new Mock<IFileSystemWrapper>();
            fsw.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Encoding>()))
                .Callback<string, string, Encoding>((path, text, enc) => { summaryEncoding = enc; })
                .Verifiable();

            var sut = new TestSummaryTransformer(fsw.Object);

            sut.Transform(new TestCaseSummary(), "outputfile");

            fsw.Verify(); // Write called
            Assert.NotNull(summaryEncoding); // encoding captured
            Assert.Empty(summaryEncoding.GetPreamble()); // encoding has no BOM preamble
            Assert.Empty(summaryEncoding.GetBytes(""));  // encoding does not write leading bytes to output
        }

        private class TestSummaryTransformer : SummaryTransformer
        {
            public TestSummaryTransformer(IFileSystemWrapper wrapper) : base(wrapper)
            {
            }

            public override string Name
            {
                get { return "Test"; }
            }

            public override string Description
            {
                get { return "Test"; }
            }

            public override string Transform(TestCaseSummary testFileSummary)
            {
                return "";
            }
        }
    }
}