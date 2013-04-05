using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Models
{
    public class TestResultFacts
    {
        [Fact]
        public void Will_exclude_line_number_from_failure_message_if_zero()
        {
            var tr = new TestResult {Message = "foo"};
            Assert.Equal("foo", tr.GetFailureMessage());
        }

        [Fact]
        public void Will_exclude_line_number_from_failure_message_if_non_zero()
        {
            var tr = new TestResult { Message = "foo", LineNumber = 2};
            Assert.Equal("foo (at line 2)", tr.GetFailureMessage());
        }
    }
}
