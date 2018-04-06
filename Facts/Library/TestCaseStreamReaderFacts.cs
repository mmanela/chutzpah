using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chutzpah.Coverage;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestCaseStreamReaderFacts
    {

        private static class JsonStreamEvents
        {

            public static string FileStartEventJsonTemplate = @"#_#FileStart#_# {{""type"": ""FileStart"", ""timetaken"":88}}
";
            public static string FileDoneEventJsonTemplate = @"#_#FileDone#_# {{""type"":""FileDone"",""timetaken"":10,""failed"":1,""passed"":2}}
";
            public static string TestStartEventJsonTemplate = @"#_#TestStart#_# {{""type"":""TestStart"",""testCase"":{{""moduleName"":""{0}"",""testName"":""{1}"",""testResults"":[]}}}}
";
            public static string TestDoneEventJsonTemplate = @"#_#TestDone#_# {{""type"":""TestDone"",""testCase"":{{""moduleName"":""{0}"",""testName"":""{1}"",""testResults"":[]}}}}
";
            public static string LogEventJsonTemplate = @"#_#Log#_# {{""type"":""Log"",""Log"":{{""message"":""{0}""}}}}
";
            public static string ErrorEventJsonTemplate = @"#_#Error#_# {{""type"":""Error"",""Error"":{{""message"":""{0}"", ""stack"":[{{""file"":""errorFile"",""function"":""errorFunc"",""line"":22}}]}}}}
";
            public static string CoverageEventJsonTemplate = @"#_#CoverageObject#_# {{""type"":""CoverageObject"",""Object"":""""}}
";

            public static string FileStartEventJson = string.Format(FileStartEventJsonTemplate);
            public static string FileDoneEventJson = string.Format(FileDoneEventJsonTemplate);
            public static string TestStartEventJson = string.Format(TestStartEventJsonTemplate, "module", "test");
            public static string TestDoneEventJson = string.Format(TestDoneEventJsonTemplate, "module", "test");
            public static string LogEventJson = string.Format(LogEventJsonTemplate, "log");
            public static string ErrorEventJson = string.Format(ErrorEventJsonTemplate, "error");
            public static string CoverageEventJson = string.Format(CoverageEventJsonTemplate);

            public static string BuildTestEventFile(params Tuple<string, string>[] moduleTestNames)
            {
                var testEvents = moduleTestNames.Select(x => string.Format(TestStartEventJsonTemplate, x.Item1, x.Item2) + string.Format(TestDoneEventJsonTemplate, x.Item1, x.Item2)).ToList();
                return FileStartEventJson + string.Join("", testEvents) + FileDoneEventJson;
            }

        }

        private class TestableTestCaseStreamReader : Testable<TestCaseStreamStringReader>
        {
            public TestableTestCaseStreamReader()
            {
            }

            public TestContext BuildContext(params string[] files)
            {
                var filesWithPosition = files.Select(x => Tuple.Create(x, "test", 1, 1)).ToList();
                return BuildContext(filesWithPosition.ToArray());
            }


            /// <summary>
            /// Build test context with file and position info.
            /// </summary>
            /// <param name="fileTestPositionInfos">
            ///     Item1: file
            ///     Item2: test
            ///     Item3: line
            ///     Item4: column
            /// </param>
            /// <returns></returns>
            public TestContext BuildContext(params Tuple<string, string, int, int>[] fileTestPositionInfos)
            {
                var referencedFiles = new List<ReferencedFile>();
                var files = fileTestPositionInfos.Select(x => x.Item1).Distinct().ToList();
                foreach (var file in files)
                {
                    var referencedFile = new ReferencedFile { Path = file, IsFileUnderTest = true };
                    var positionsForFile = fileTestPositionInfos.Where(x => x.Item1 == file);
                    foreach (var position in positionsForFile)
                    {
                        referencedFile.FilePositions.Add(position.Item3, position.Item4, position.Item2);
                    }
                    referencedFiles.Add(referencedFile);
                }

                if (files != null && files.Count > 0)
                {
                    return new TestContext
                    {
                        InputTestFiles = files,
                        InputTestFilesString = string.Join(",", files),
                        ReferencedFiles = referencedFiles
                    };
                }
                else
                {
                    return new TestContext();
                }
            }
        }

        private class WaitingStreamReader : StreamReader
        {
            private readonly int waitTime;

            public WaitingStreamReader(Stream stream, int waitTime)
                : base(stream)
            {
                this.waitTime = waitTime;
            }

            public override string ReadLine()
            {
                Thread.Sleep(waitTime);
                return null;
            }

            public override Task<string> ReadLineAsync()
            {
                return Task.Delay(waitTime).ContinueWith(x => (string)null);
            }
        }

        public class Read
        {
            [Fact]
            public void Will_throw_argument_null_exception_if_stream_is_null()
            {
                var reader = new TestableTestCaseStreamReader();

                var model = Record.Exception(() => reader.ClassUnderTest.Read(null, new TestOptions(), new TestContext(), null)) as ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_throw_argument_null_exception_if_testoptions_is_null()
            {
                var reader = new TestableTestCaseStreamReader();
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, new StreamReader(new MemoryStream()), 1000);

                var model = Record.Exception(() => reader.ClassUnderTest.Read(processStream, null, new TestContext(), null)) as ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_throw_argument_null_exception_if_context_is_null()
            {
                var reader = new TestableTestCaseStreamReader();
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, new StreamReader(new MemoryStream()), 1000);

                var model = Record.Exception(() => reader.ClassUnderTest.Read(processStream, new TestOptions(), null, null)) as ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_fire_file_started_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                callback.Verify(x => x.FileStarted("file"));
            }

            [Fact]
            public void Will_fire_file_finished_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.TestDoneEventJson + JsonStreamEvents.FileDoneEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestFileSummary result = null;
                callback.Setup(x => x.FileFinished("file", It.IsAny<TestFileSummary>())).Callback<string, TestFileSummary>((f, t) => result = t); ;

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal(10, result.TimeTaken);

            }

            [Fact]
            public void Will_fire_test_started_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestStarted(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal("file", result.InputTestFile);
            }

            [Fact]
            public void Will_fire_test_finished_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.TestDoneEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestFinished(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal("file", result.InputTestFile);
            }

            [Fact]
            public void Will_fire_log_event_if_file_is_already_known()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.LogEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestLog result = null;
                callback.Setup(x => x.FileLog(It.IsAny<TestLog>())).Callback<TestLog>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal("log", result.Message);
                Assert.Equal("file", result.InputTestFile);
            }

            [Fact]
            public void Will_fire_log_event_if_file_is_not_already_known()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.LogEventJson + JsonStreamEvents.TestStartEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestLog result = null;
                callback.Setup(x => x.FileLog(It.IsAny<TestLog>())).Callback<TestLog>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal("log", result.Message);
                Assert.Equal("file", result.InputTestFile);
            }

            [Fact]
            public void Will_supress_internal_log_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + string.Format(JsonStreamEvents.LogEventJsonTemplate, "!!_!! log");
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestLog result = null;
                callback.Setup(x => x.FileLog(It.IsAny<TestLog>())).Callback<TestLog>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.Null(result);
            }

            [Fact]
            public void Will_fire_error_event()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.ErrorEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestError result = null;
                callback.Setup(x => x.FileError(It.IsAny<TestError>())).Callback<TestError>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal("file", result.InputTestFile);
                Assert.Equal("error", result.Message);
                Assert.Equal("errorFile", result.Stack[0].File);
                Assert.Equal("errorFunc", result.Stack[0].Function);
                Assert.Equal("22", result.Stack[0].Line);
            }

            [Fact]
            public void Will_put_test_case_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + @"
#_#TestDone#_# {""type"":""TestDone"",""testCase"":{""moduleName"":""module"",""testName"":""test"",""testResults"":[{""message"":""bad"",""passed"":false,""actual"":4,""expected"":5}]}}
";
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object).TestFileSummaries[0];


                Assert.Equal(1, summary.Tests.Count);
                Assert.Equal(1, summary.Tests[0].TestResults.Count);
                Assert.Equal("file", summary.Tests[0].InputTestFile);
                Assert.Equal("module", summary.Tests[0].ModuleName);
                Assert.Equal("test", summary.Tests[0].TestName);
                Assert.False(summary.Tests[0].ResultsAllPassed);
                Assert.False(summary.Tests[0].TestResults[0].Passed);
                Assert.Equal("4", summary.Tests[0].TestResults[0].Actual);
                Assert.Equal("5", summary.Tests[0].TestResults[0].Expected);
                Assert.Equal("bad", summary.Tests[0].TestResults[0].Message);
            }

            [Fact]
            public void Will_put_logs_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + string.Format(JsonStreamEvents.LogEventJsonTemplate, "hi") + string.Format(JsonStreamEvents.LogEventJsonTemplate, "bye");
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object).TestFileSummaries[0];

                Assert.Equal(2, summary.Logs.Count);
                Assert.Equal("file", summary.Logs[0].InputTestFile);
                Assert.Equal("hi", summary.Logs[0].Message);
                Assert.Equal("bye", summary.Logs[1].Message);
            }

            [Fact]
            public void Will_put_error_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.ErrorEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object).TestFileSummaries[0];

                Assert.Equal(1, summary.Errors.Count);
                Assert.Equal("file", summary.Errors[0].InputTestFile);
                Assert.Equal("error", summary.Errors[0].Message);
                Assert.Equal("errorFile", summary.Errors[0].Stack[0].File);
                Assert.Equal("errorFunc", summary.Errors[0].Stack[0].Function);
                Assert.Equal("22", summary.Errors[0].Stack[0].Line);
            }

            [Fact]
            public void Will_not_set_empty_coverage_object_when_coverage_is_disabled()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"";
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions { CoverageOptions = new CoverageOptions { Enabled = false } }, context, callback.Object).TestFileSummaries[0];

                Assert.Null(summary.CoverageObject);
            }

            [Fact]
            public void Will_set_empty_coverage_object_when_coverage_is_enabled()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"";
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions { CoverageOptions = new CoverageOptions { Enabled = true } }, context, callback.Object).TestFileSummaries[0];

                Assert.NotNull(summary.CoverageObject);
            }

            [Fact]
            public void Will_put_coverage_object_in_summary()
            {
                var reader = new TestableTestCaseStreamReader();
                var context = reader.BuildContext("file");
                var coverageEngine = new Mock<ICoverageEngine>();
                coverageEngine.Setup(ce => ce.DeserializeCoverageObject(It.IsAny<string>(), context))
                              .Returns(new CoverageData());
                context.CoverageEngine = coverageEngine.Object;
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.CoverageEventJson;
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object).TestFileSummaries[0];

                Assert.NotNull(summary.CoverageObject);
            }

            [Fact]
            public void Will_recover_after_malformed_json()
            {
                var reader = new TestableTestCaseStreamReader();

                var json = @"
#_#Log#_# ""type"":""Log"",""Log"":{""message"":""hi""}}
"
+ string.Format(JsonStreamEvents.LogEventJsonTemplate, "bye") + JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson;
                var context = reader.BuildContext("file");
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object).TestFileSummaries[0];

                Assert.Equal(1, summary.Logs.Count);
                Assert.Equal("file", summary.Logs[0].InputTestFile);
                Assert.Equal("bye", summary.Logs[0].Message);
            }

            [Fact]
            public void Will_get_map_line_numbers_to_test_result()
            {
                var reader = new TestableTestCaseStreamReader();
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.TestDoneEventJson;
                var referencedFile = new ReferencedFile
                {
                    IsFileUnderTest = true,
                    Path = "inputTestFile",
                    FilePositions = new FilePositions()
                };
                referencedFile.FilePositions.Add(1, 3, "test");
                var context = new TestContext
                {
                    TestHarnessPath = "htmlTestFile",
                    ReferencedFiles = new[] { referencedFile }
                };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestFinished(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

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
                var json = JsonStreamEvents.FileStartEventJson + JsonStreamEvents.TestStartEventJson + JsonStreamEvents.TestDoneEventJson;
                var referencedFile = new ReferencedFile
                {
                    IsFileUnderTest = true,
                    Path = "inputTestFile",
                    FilePositions = new FilePositions()
                };
                var context = new TestContext
                {
                    TestHarnessPath = "htmlTestFile",
                    ReferencedFiles = new[] { referencedFile }
                };
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                TestCase result = null;
                callback.Setup(x => x.TestFinished(It.IsAny<TestCase>())).Callback<TestCase>(t => result = t);

                reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.NotNull(result);
                Assert.Equal("module", result.ModuleName);
                Assert.Equal("test", result.TestName);
                Assert.Equal(0, result.Line);
                Assert.Equal(0, result.Column);
            }

            [Fact]
            public void Will_set_timed_out_after_test_file_timeout_and_kill_process()
            {
                var reader = new TestableTestCaseStreamReader();

                var context = reader.BuildContext("file");
                var stream = new WaitingStreamReader(new MemoryStream(Encoding.UTF8.GetBytes("")), 10000);
                var process = new Mock<IProcessWrapper>();
                var processStream = new ProcessStreamStringSource(process.Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions { TestFileTimeoutMilliseconds = 200 }, context, callback.Object);

                Assert.NotNull(summary);
                Assert.True(summary.TimedOut);
                process.Verify(x => x.Kill());
            }

            [Fact]
            public void Will_supress_errors_after_timeout_when_killing_process()
            {
                var reader = new TestableTestCaseStreamReader();

                var context = reader.BuildContext("file");
                var stream = new WaitingStreamReader(new MemoryStream(Encoding.UTF8.GetBytes("")), 10000);
                var process = new Mock<IProcessWrapper>();
                var processStream = new ProcessStreamStringSource(process.Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                process.Setup(x => x.Kill()).Throws(new InvalidOperationException());

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions { TestFileTimeoutMilliseconds = 200 }, context, callback.Object).TestFileSummaries[0];

                Assert.NotNull(summary);
                process.Verify(x => x.Kill());
            }

            [Fact]
            public void Will_use_timeout_from_context_if_available()
            {
                var reader = new TestableTestCaseStreamReader();
                var context = reader.BuildContext("file");
                context.TestFileSettings = new ChutzpahTestSettingsFile { TestFileTimeout = 200 };
                var stream = new WaitingStreamReader(new MemoryStream(Encoding.UTF8.GetBytes("")), 10000);
                var process = new Mock<IProcessWrapper>();
                var processStream = new ProcessStreamStringSource(process.Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();
                process.Setup(x => x.Kill()).Throws(new InvalidOperationException());

                var summary = reader.ClassUnderTest.Read(processStream, new TestOptions { TestFileTimeoutMilliseconds = 2000 }, context, callback.Object).TestFileSummaries[0];

                Assert.NotNull(summary);
                process.Verify(x => x.Kill());
            }

            [Fact]
            public void Will_place_ambiguous_test_in_first_context_given_no_matches_and_no_current_file_match()
            {
                // This case covers the scenario where there are two files and each has the same test name
                // what should happen is when we hit the first one we don't know which file it is from so we *assume* the first 
                // one. Then when we find the same test name again we realize we can't assign it to the current file
                // so we assign it to the next

                var reader = new TestableTestCaseStreamReader();

                var jsonFile1 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("", "test1"));
                var jsonFile2 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("", "test1"));
                var json = jsonFile1 + jsonFile2;
                var context = reader.BuildContext(Tuple.Create("file1", "test1", 1, 1), Tuple.Create("file2", "test1", 2, 2));
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summaries = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.Equal(1, summaries.TestFileSummaries[0].Tests.Count);
                Assert.Equal("file1", summaries.TestFileSummaries[0].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[0].Tests[0].TestName);
                Assert.Equal(1, summaries.TestFileSummaries[1].Tests.Count);
                Assert.Equal("file2", summaries.TestFileSummaries[1].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[1].Tests[0].TestName);
            }

            [Fact]
            public void Will_place_not_found_test_in_first_context_given_no_matches_and_no_current_file_match()
            {
                // This case covers the scenario where the test names are not found in filePosition map
                // what should happen is when we hit the first one we don't know which file it is from so we *assume* the first 
                // one. Then when we find the same test name again we realize we can't assign it to the current file
                // so we assign it to the next

                var reader = new TestableTestCaseStreamReader();

                var jsonFile1 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("", "test1"));
                var jsonFile2 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("", "test1"));
                var json = jsonFile1 + jsonFile2;
                var context = reader.BuildContext(Tuple.Create("file1", "testNoMatch", 1, 1), Tuple.Create("file2", "testNoMatch", 2, 2));
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summaries = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.Equal(1, summaries.TestFileSummaries[0].Tests.Count);
                Assert.Equal("file1", summaries.TestFileSummaries[0].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[0].Tests[0].TestName);
                Assert.Equal(1, summaries.TestFileSummaries[1].Tests.Count);
                Assert.Equal("file2", summaries.TestFileSummaries[1].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[1].Tests[0].TestName);
            }

            [Fact]
            public void Will_place_in_correct_file_given_module_names()
            {
                // This case covers the scenario where the test names are not found in filePosition map
                // what should happen is when we hit the first one we don't know which file it is from so we *assume* the first 
                // one. Then when we find the same test name again we realize we can't assign it to the current file
                // so we assign it to the next

                var reader = new TestableTestCaseStreamReader();

                var jsonFile1 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("module1", "test1"), Tuple.Create("module2", "test1"));
                var jsonFile2 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("module1", "test1"), Tuple.Create("module2", "test1"));
                var jsonFile3 = JsonStreamEvents.BuildTestEventFile(Tuple.Create("module1", "test1"), Tuple.Create("module2", "test1"));
                var json = jsonFile1 + jsonFile2 + jsonFile3;
                var context = reader.BuildContext(Tuple.Create("file1", "test1", 1, 1), Tuple.Create("file1", "test1", 2, 1),
                    Tuple.Create("file2", "test1", 1, 1), Tuple.Create("file2", "test1", 2, 1),
                    Tuple.Create("file3", "test1", 1, 1), Tuple.Create("file3", "test1", 2, 1));
                var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(json)));
                var processStream = new ProcessStreamStringSource(new Mock<IProcessWrapper>().Object, stream, 1000);
                var callback = new Mock<ITestMethodRunnerCallback>();

                var summaries = reader.ClassUnderTest.Read(processStream, new TestOptions(), context, callback.Object);

                Assert.Equal(2, summaries.TestFileSummaries[0].Tests.Count);
                Assert.Equal("file1", summaries.TestFileSummaries[0].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[0].Tests[0].TestName);
                Assert.Equal("test1", summaries.TestFileSummaries[0].Tests[1].TestName);

                Assert.Equal(2, summaries.TestFileSummaries[1].Tests.Count);
                Assert.Equal("file2", summaries.TestFileSummaries[1].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[1].Tests[0].TestName);
                Assert.Equal("test1", summaries.TestFileSummaries[1].Tests[1].TestName);

                Assert.Equal(2, summaries.TestFileSummaries[2].Tests.Count);
                Assert.Equal("file3", summaries.TestFileSummaries[2].Tests[0].InputTestFile);
                Assert.Equal("test1", summaries.TestFileSummaries[2].Tests[0].TestName);
                Assert.Equal("test1", summaries.TestFileSummaries[2].Tests[1].TestName);

            }
        }
    }

}