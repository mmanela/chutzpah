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
        public void Will_include_line_number_in_failure_message_if_non_zero()
        {
            var tr = new TestResult { Message = "foo", LineNumber = 2};
            Assert.Equal("foo (on line 2)", tr.GetFailureMessage());
        }

        [Fact]
        public void Will_include_stack_trace_in_failure_message_if_non_null()
        {
            var tr = new TestResult { Message = "foo", StackTrace = "at foo.js:22"};
            Assert.Equal("foo\n\tat foo.js:22", tr.GetFailureMessage());
        }
    }
}
