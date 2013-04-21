using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Models
{
    public class TestResultFacts
    {
        [Fact]
        public void Will_include_stack_trace_in_failure_message_if_non_null()
        {
            var tr = new TestResult { Message = "foo", StackTrace = "at foo.js:22"};
            Assert.Equal("foo\n\tat foo.js:22", tr.GetFailureMessage());
        }
    }
}
