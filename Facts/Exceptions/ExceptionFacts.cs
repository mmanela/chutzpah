using Chutzpah.Exceptions;
using Xunit;

namespace Chutzpah.Facts.Exceptions
{
    public class ExceptionFacts
    {
        public class ChutzpahCompilationFailedExceptionFacts
        {
            [Fact]
            public void Its_ToString_returns_the_message_by_default()
            {
                var ex = new ChutzpahCompilationFailedException("foo");
                Assert.Equal("foo", ex.ToString());
            }

            [Fact]
            public void Its_ToString_includes_the_filename_if_set()
            {
                var ex = new ChutzpahCompilationFailedException("foo");
                ex.SourceFile = "test.coffee";
                Assert.Contains(" [in file test.coffee]", ex.ToString());
            }

            [Fact]
            public void Its_ToString_excludes_the_stack_trace()
            {
                var ex = Record.Exception(() => { throw new ChutzpahCompilationFailedException("foo"); });
                Assert.DoesNotContain("at Chutzpah.Facts.Exceptions", ex.ToString());
            }
        }
    }
}
