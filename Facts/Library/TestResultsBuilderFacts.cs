using System;
using System.Linq;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts
{
    public class TestResultsBuilderFacts
    {
        private class TestableTestResultsBuilder : TestResultsBuilder
        {
            public Mock<IJsonSerializer> MoqJsonSerializer { get; set; }
            public Mock<IHtmlUtility> MoqHtmlUtility { get; set; }

            private TestableTestResultsBuilder(Mock<IJsonSerializer> moqJsonSerializer, Mock<IHtmlUtility> moqHtmlUtility)
                : base(moqJsonSerializer.Object, moqHtmlUtility.Object)
            {
                MoqJsonSerializer = moqJsonSerializer;
                MoqHtmlUtility = moqHtmlUtility;
            }

            public static TestableTestResultsBuilder Create()
            {
                var builder = new TestableTestResultsBuilder(new Mock<IJsonSerializer>(), new Mock<IHtmlUtility>());

                return builder;
            }
        }

        public class Build
        {
            [Fact]
            public void Will_throw_argument_null_exception_if_browser_result_is_null()
            {
                var builder = TestableTestResultsBuilder.Create();

                var model = Record.Exception(() => builder.Build(null)) as ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_argument_exception_if_browser_result_json_is_null()
            {
                var builder = TestableTestResultsBuilder.Create();

                var model =
                    Record.Exception(() => builder.Build(new BrowserTestFileResult(new TestContext(), null))) as
                    ArgumentNullException;

                Assert.NotNull(model);
            }

            [Fact]
            public void Will_parse_json_out_of_browser_result()
            {
                var builder = TestableTestResultsBuilder.Create();
                var json = @"#_#Begin#_#
                            json
                            #_#End#_#";
                builder.MoqJsonSerializer.Setup(x => x.Deserialize<JsonTestOutput>("json")).Returns(new JsonTestOutput()).Verifiable();

                builder.Build(new BrowserTestFileResult(new TestContext(), json));

                builder.MoqJsonSerializer.Verify();
            }

            [Fact]
            public void Will_map_deserialized_json_to_test_result_model()
            {
                var builder = TestableTestResultsBuilder.Create();
                builder.MoqHtmlUtility
                    .Setup(x => x.DecodeJavaScript(It.IsAny<string>()))
                    .Returns<string>(x => "Decoded " + x);
                builder.MoqJsonSerializer
                    .Setup(x => x.Deserialize<JsonTestOutput>("json"))
                    .Returns(new JsonTestOutput
                                 {
                                     Results = new[]
                                                   {
                                                       new JsonTestCase
                                                           {
                                                               State = "pass",
                                                               Name = "name1"
                                                           },
                                                       new JsonTestCase
                                                           {
                                                               State = "fail",
                                                               Name = "name2",
                                                               Module = "module2",
                                                               Expected = "10",
                                                               Actual = "4",
                                                               Message = "message"
                                                           }
                                                   }
                                 });

                var tests = builder.Build(new BrowserTestFileResult(new TestContext { TestHarnessPath = "htmlTestFile", InputTestFile = "inputTestFile" }, "json"));

                Assert.Equal(2, tests.Count());
                Assert.True(tests.ElementAt(0).Passed);
                Assert.Equal("Decoded name1", tests.ElementAt(0).TestName);
                Assert.Equal("htmlTestFile", tests.ElementAt(0).HtmlTestFile);
                Assert.Equal("inputTestFile", tests.ElementAt(0).InputTestFile);

                Assert.False(tests.ElementAt(1).Passed);
                Assert.Equal("Decoded name2", tests.ElementAt(1).TestName);
                Assert.Equal("Decoded module2", tests.ElementAt(1).ModuleName);
                Assert.Equal("Decoded 10", tests.ElementAt(1).Expected);
                Assert.Equal("Decoded 4", tests.ElementAt(1).Actual);
                Assert.Equal("Decoded message", tests.ElementAt(1).Message);
                Assert.Equal("htmlTestFile", tests.ElementAt(1).HtmlTestFile);
                Assert.Equal("inputTestFile", tests.ElementAt(1).InputTestFile);
            }

            [Fact]
            public void Will_get_map_line_numbers_to_test_result()
            {
                var builder = TestableTestResultsBuilder.Create();
                builder.MoqHtmlUtility
                    .Setup(x => x.DecodeJavaScript(It.IsAny<string>()))
                    .Returns<string>(x => x);
                var referencedFile = new ReferencedFile
                {
                    IsFileUnderTest = true,
                    Path = "inputTestFile",
                    FilePositions = new FilePositions()
                };
                referencedFile.FilePositions.Add("module", "name", 1, 3);
                builder.MoqJsonSerializer
                    .Setup(x => x.Deserialize<JsonTestOutput>("json"))
                    .Returns(new JsonTestOutput
                                 {
                                     Results = new[]
                                                   {
                                                       new JsonTestCase
                                                           {
                                                               State = "fail",
                                                               Name = "name",
                                                               Module = "module",
                                                               Expected = "10",
                                                               Actual = "4",
                                                               Message = "message"
                                                           }
                                                   }
                                 });

                var tests = builder.Build(new BrowserTestFileResult(new TestContext
                {
                    TestHarnessPath = "htmlTestFile",
                    InputTestFile = "inputTestFile",
                    ReferencedJavaScriptFiles = new[] { referencedFile }
                }, "json"));

                var test = tests.ElementAt(0);
                Assert.Equal(1, test.Line);
                Assert.Equal(3, test.Column);
            }

            [Fact]
            public void Will_set_line_number_to_zero_if_not_file_positons_exist()
            {
                var builder = TestableTestResultsBuilder.Create();
                builder.MoqHtmlUtility
                    .Setup(x => x.DecodeJavaScript(It.IsAny<string>()))
                    .Returns<string>(x => x);
                var referencedFile = new ReferencedFile
                {
                    IsFileUnderTest = false,
                    Path = "inputTestFile",
                    FilePositions = new FilePositions()
                };
                referencedFile.FilePositions.Add("module", "name", 1, 3);
                builder.MoqJsonSerializer
                    .Setup(x => x.Deserialize<JsonTestOutput>("json"))
                    .Returns(new JsonTestOutput
                    {
                        Results = new[]
                                                   {
                                                       new JsonTestCase
                                                           {
                                                               State = "fail",
                                                               Name = "name",
                                                               Module = "module",
                                                               Expected = "10",
                                                               Actual = "4",
                                                               Message = "message"
                                                           }
                                                   }
                    });

                var tests = builder.Build(new BrowserTestFileResult(new TestContext
                {
                    TestHarnessPath = "htmlTestFile",
                    InputTestFile = "inputTestFile",
                    ReferencedJavaScriptFiles = new[] { referencedFile }
                }, "json"));

                var test = tests.ElementAt(0);
                Assert.Equal(0, test.Line);
                Assert.Equal(0, test.Column);
            }
        }
    }
}