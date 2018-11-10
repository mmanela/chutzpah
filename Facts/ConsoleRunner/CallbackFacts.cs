using System;
using System.IO;
using System.Threading.Tasks;
using Chutzpah.Models;
using Chutzpah.RunnerCallbacks;
using Xunit;

namespace Chutzpah.Facts.ConsoleRunner
{
    public class CallbackFacts
    {
        public class ConsoleRedirector : IDisposable
        {
            protected readonly TextWriter _out;
            protected readonly TextWriter _err;
            private readonly TextWriter _oldOut;
            private readonly TextWriter _oldErr;

            public ConsoleRedirector()
            {
                _oldOut = Console.Out;
                _oldErr = Console.Error;
                Console.SetOut(_out = new StringWriter());
                Console.SetError(_err = new StringWriter());
            }

            public void Dispose()
            {
                Console.SetOut(_oldOut);
                Console.SetError(_oldErr);
            }
        }

        public class StandardConsoleRunnerCallbackFacts : ConsoleRedirector
        {
            [Fact]
            public void It_will_write_file_log_messages_to_console()
            {
                var ctx = new TestContext { TaskId = 99 };
                var cb = new StandardConsoleRunnerCallback(false, false, false, false);
                var log = new TestLog {InputTestFile = "test.js", Message = "hello"};
                cb.FileLog(ctx, log);

                Assert.Equal("Log Message: hello from test.js", _out.ToString().Trim());
            }

        }

        public class TeamCityConsoleRunnerCallbackFacts : ConsoleRedirector
        {
            [Fact]
            public void It_will_write_file_log_messages_as_standard_out_for_passed_test()
            {
                var ctx = new TestContext { TaskId = 1 };
                var cb = new TeamCityConsoleRunnerCallback();
                var log = new TestLog {InputTestFile = "test.js", Message = "hello"};
                var result = new TestResult {Passed = true};
                var tc = new TestCase {TestName = "foo", TestResults = new[] {result}};
                cb.TestStarted(ctx, tc);
                cb.FileLog(ctx, log);
                cb.TestFinished(ctx, tc);

                Assert.Contains($"##teamcity[testStdOut name='foo' flowId='1' out='Log Message: hello from test.js|n|nPassed']", _out.ToString());
            }

            [Fact]
            public void It_will_write_file_log_messages_as_standard_out_for_failed_test()
            {
                var ctx = new TestContext { TaskId = 65 };
                var cb = new TeamCityConsoleRunnerCallback();
                var log = new TestLog { InputTestFile = "test.js", Message = "hello" };
                var result = new TestResult {Passed = false, Message = "failure"};
                var tc = new TestCase { TestName = "foo", TestResults = new[] { result } };
                cb.TestStarted(ctx, tc);
                cb.FileLog(ctx, log);
                cb.TestFinished(ctx, tc);

                Assert.Contains($"##teamcity[testStdOut name='foo' flowId='65' out='Log Message: hello from test.js|n|nTest |'foo|' failed|n\tfailure|nin  (line 0)|n|n']", _out.ToString());
            }

            [Fact]
            public void It_will_separate_file_log_messages_per_test()
            {
                var ctx = new TestContext { TaskId = 88 };
                var cb = new TeamCityConsoleRunnerCallback();
                var log1 = new TestLog {InputTestFile = "test.js", Message = "hello"};
                var log2 = new TestLog {InputTestFile = "test.js", Message = "world"};
                var result = new TestResult { Passed = true };
                var tc1 = new TestCase {TestName = "foo", TestResults = new[] {result}};
                var tc2 = new TestCase {TestName = "bar", TestResults = new[] {result}};

                cb.TestStarted(ctx, tc1);
                cb.FileLog(ctx, log1);
                cb.TestFinished(ctx, tc1);

                cb.TestStarted(ctx, tc2);
                cb.FileLog(ctx, log2);
                cb.TestFinished(ctx, tc2);

                Assert.Contains($"##teamcity[testStdOut name='foo' flowId='88' out='Log Message: hello from test.js|n|nPassed']", _out.ToString());
                Assert.Contains($"##teamcity[testStdOut name='bar' flowId='88' out='Log Message: world from test.js|n|nPassed']", _out.ToString());
            }
        }
    }
}
