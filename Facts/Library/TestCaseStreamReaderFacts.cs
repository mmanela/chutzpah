using System;
using System.IO;
using System.Linq;
using System.Text;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestCaseStreamReaderFacts
    {
        private class TestableTestCaseStreamReader : Testable<TestCaseStreamReader>
        {
            public TestableTestCaseStreamReader()
            {
            }
        }


        public class Read
        {
            [Fact]
            public void Will_throw_argument_null_exception_if_stream_is_null()
            {
                var reader = new TestableTestCaseStreamReader();

                var model = Record.Exception(() => reader.ClassUnderTest.Read(null, new TestContext(), null, true)) as ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_throw_argument_null_exception_if_context_is_null()
            {
                var reader = new TestableTestCaseStreamReader();
                var stream = new StreamReader(new MemoryStream());

                var model = Record.Exception(() => reader.ClassUnderTest.Read(stream, null, null, true)) as ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_fire_file_started_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = @"#_#FileStart#_# {""type"": ""FileStart""}";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();

                reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                callback.Verify(x => x.FileStarted("file"));
            }

            [Fact]
            public void Will_fire_file_finished_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = @"#_#FileDone#_# {""type"":""FileDone"",""timetaken"":10,""failed"":1,""passed"":2}";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();

                reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                callback.Verify(x => x.FileFinished("file", It.IsAny<TestCaseSummary>()));
            }

            [Fact]
            public void Will_fire_test_started_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = @"#_#TestStart#_# {""type"":""TestStart"",""testCase"":{""moduleName"":""module"",""testName"":""test"",""testResults"":[]}}";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestStarted(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal("file", result.InputTestFile);
            }

            [Fact]
            public void Will_fire_test_finished_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = @"#_#TestDone#_# {""type"":""TestDone"",""testCase"":{""moduleName"":""module"",""testName"":""test"",""testResults"":[]}}";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestFinished(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal("file", result.InputTestFile);
            }

            [Fact]
            public void Will_put_test_case_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"
#_#TestDone#_# {""type"":""TestDone"",""testCase"":{""moduleName"":""module"",""testName"":""test"",""testResults"":[{""message"":""bad"",""passed"":false,""actual"":4,""expected"":5}]}}
";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(stream, context, callback.Object, false);


                Assert.Equal(1, summary.Tests.Count);
                Assert.Equal(1, summary.Tests[0].TestResults.Count);
                Assert.Equal("file", summary.Tests[0].InputTestFile);
                Assert.Equal("module", summary.Tests[0].ModuleName);
                Assert.Equal("test", summary.Tests[0].TestName);
                Assert.False(summary.Tests[0].Passed);
                Assert.False(summary.Tests[0].TestResults[0].Passed);
                Assert.Equal("4", summary.Tests[0].TestResults[0].Actual);
                Assert.Equal("5", summary.Tests[0].TestResults[0].Expected);
                Assert.Equal("bad", summary.Tests[0].TestResults[0].Message);
            }

            [Fact]
            public void Will_put_logs_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"
#_#Log#_# {""type"":""Log"",""Log"":{""message"":""hi""}}
#_#Log#_# {""type"":""Log"",""Log"":{""message"":""bye""}}
";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                Assert.Equal(2, summary.Logs.Count);
                Assert.Equal("file", summary.Logs[0].InputTestFile);
                Assert.Equal("hi", summary.Logs[0].Message);
                Assert.Equal("bye", summary.Logs[1].Message);
            }

            [Fact]
            public void Will_put_error_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"
#_#Error#_# {""type"":""Error"",""Error"":{""message"":""uhoh"", ""stack"":[{""file"":""errorFile"",""function"":""errorFunc"",""line"":22}]}}
";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(stream, context, callback.Object, false);
                
                Assert.Equal(1, summary.Errors.Count);
                Assert.Equal("file", summary.Errors[0].InputTestFile);
                Assert.Equal("uhoh", summary.Errors[0].Message);
                Assert.Equal("errorFile", summary.Errors[0].Stack[0].File);
                Assert.Equal("errorFunc", summary.Errors[0].Stack[0].Function);
                Assert.Equal("22", summary.Errors[0].Stack[0].Line);
            }

            [Fact]
            public void Will_recover_after_malformed_json()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"
#_#Log#_# ""type"":""Log"",""Log"":{""message"":""hi""}}
#_#Log#_# {""type"":""Log"",""Log"":{""message"":""bye""}}
";
                var context = new TestContext { InputTestFile = "file" };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                Assert.Equal(1, summary.Logs.Count);
                Assert.Equal("file", summary.Logs[0].InputTestFile);
                Assert.Equal("bye", summary.Logs[0].Message);
            }

            [Fact]
            public void Will_get_map_line_numbers_to_test_result()
            {
                var reader = new TestableTestCaseStreamReader(); 
                var json = @"#_#TestDone#_# {""type"":""TestDone"",""testCase"":{""moduleName"":""module"",""testName"":""test"",""testResults"":[]}}";
                var referencedFile = new ReferencedFile
                {
                    IsFileUnderTest = true,
                    Path = "inputTestFile",
                    FilePositions = new FilePositions()
                };
                referencedFile.FilePositions.Add(1, 3);
                var context = new TestContext
                {
                    TestHarnessPath = "htmlTestFile",
                    InputTestFile = "inputTestFile",
                    ReferencedJavaScriptFiles = new[] { referencedFile }
                };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestFinished(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal(1, result.Line);
                Assert.Equal(3, result.Column);
            }

            [Fact]
            public void Will_set_line_position_to_zero_when_no_matching_file_position()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = @"#_#TestDone#_# {""type"":""TestDone"",""testCase"":{""moduleName"":""module"",""testName"":""test"",""testResults"":[]}}";
                var referencedFile = new ReferencedFile
                {
                    IsFileUnderTest = true,
                    Path = "inputTestFile",
                    FilePositions = new FilePositions()
                };
                var context = new TestContext
                {
                    TestHarnessPath = "htmlTestFile",
                    InputTestFile = "inputTestFile",
                    ReferencedJavaScriptFiles = new[] { referencedFile }
                };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestFinished(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(stream, context, callback.Object, false);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal(0, result.Line);
                Assert.Equal(0, result.Column);
            }
        }
    }
  
}