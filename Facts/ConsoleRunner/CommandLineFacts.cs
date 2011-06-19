using System;
using Xunit;

namespace Chutzpah.Facts.ConsoleRunner
{
    public class CommandLineFacts
    {
        private class TestableCommandLine : CommandLine
        {
            private TestableCommandLine(string[] arguments)
                : base(arguments)
            {
            }

            public static TestableCommandLine Create(string[] arguments)
            {
                return new TestableCommandLine(arguments);
            }
        }

        public class InvalidOptionFacts
        {
            [Fact]
            public void OptionWithoutSlashThrows()
            {
                var arguments = new[] {"file.html", "teamcity"};

                var exception = Record.Exception(() => TestableCommandLine.Create(arguments));

                Assert.IsType<ArgumentException>(exception);
                Assert.Equal("unknown command line option: teamcity", exception.Message);
            }
        }

        public class FileOptionFacts
        {
            [Fact]
            public void First_Argument_With_No_Slash_Is_Added_As_File()
            {
                var arguments = new[] {"test.html"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.Contains("test.html", commandLine.Files);
            }

            [Fact]
            public void File_Option_Adds_File()
            {
                var arguments = new[] {"/file", "test.html"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.Contains("test.html", commandLine.Files);
            }

            [Fact]
            public void File_Option_Ignores_Case()
            {
                string[] arguments = new[] {"/fIlE", "test.html"};

                TestableCommandLine commandLine = TestableCommandLine.Create(arguments);

                Assert.Contains("test.html", commandLine.Files);
            }
        }


        public class DebugOptionFacts
        {
            [Fact]
            public void Debug_Option_Not_Passed_Debug_False()
            {
                var arguments = new[] { "test.html" };

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.False(commandLine.Debug);
            }

            [Fact]
            public void Debug_Option_Debug_Is_True()
            {
                var arguments = new[] { "test.html", "/debug" };

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.Debug);
            }

            [Fact]
            public void Debug_Option_Ignore_Case_Debug_Is_True()
            {
                var arguments = new[] { "test.html", "/dEbuG" };

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.Debug);
            }
        }

        public class SilentOptionFacts
        {
            [Fact]
            public void Silent_Option_Not_Passed_Silent_False()
            {
                var arguments = new[] {"test.html"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.False(commandLine.Silent);
            }

            [Fact]
            public void Silent_Option_Silent_Is_True()
            {
                var arguments = new[] {"test.html", "/silent"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.Silent);
            }

            [Fact]
            public void Silent_Option_Ignore_Case_Silent_Is_True()
            {
                var arguments = new[] {"test.html", "/sIlEnT"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.Silent);
            }
        }

        public class WaitOptionFacts
        {
            [Fact]
            public void Wait_Option_Not_Passed_Wait_False()
            {
                var arguments = new[] {"test.html"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.False(commandLine.Wait);
            }

            [Fact]
            public void Wait_Option_Wait_Is_True()
            {
                var arguments = new[] {"test.html", "/wait"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.Wait);
            }

            [Fact]
            public void Wait_Option_Ignore_Case_Wait_Is_True()
            {
                var arguments = new[] {"test.html", "/wAiT"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.Wait);
            }
        }

        public class TeamCityArgumentFacts
        {
            [Fact, TeamCityEnvironmentRestore]
            public void TeamCity_Option_Not_Passed_TeamCity_False()
            {
                var arguments = new[] {"test.html"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.False(commandLine.TeamCity);
            }

            [Fact, TeamCityEnvironmentRestore(Value = "TeamCity")]
            public void TeamCity_Option_Not_Passed_Environment_Set_TeamCity_True()
            {
                var arguments = new[] {"test.html"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.TeamCity);
            }

            [Fact, TeamCityEnvironmentRestore]
            public void TeamCity_Option_TeamCity_True()
            {
                var arguments = new[] {"test.html", "/teamcity"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.TeamCity);
            }

            [Fact, TeamCityEnvironmentRestore]
            public void TeamCity_Option_Ignore_Case_TeamCity_True()
            {
                var arguments = new[] {"test.html", "/tEaMcItY"};

                var commandLine = TestableCommandLine.Create(arguments);

                Assert.True(commandLine.TeamCity);
            }

            private class TeamCityEnvironmentRestore : BeforeAfterTestAttribute
            {
                private string originalValue;

                public string Value { get; set; }

                public override void Before(System.Reflection.MethodInfo methodUnderTest)
                {
                    originalValue = Environment.GetEnvironmentVariable("TEAMCITY_PROJECT_NAME");
                    Environment.SetEnvironmentVariable("TEAMCITY_PROJECT_NAME", Value);
                }

                public override void After(System.Reflection.MethodInfo methodUnderTest)
                {
                    Environment.SetEnvironmentVariable("TEAMCITY_PROJECT_NAME", originalValue);
                }
            }
        }
    }
}