namespace Chutzpah.Facts.Library
{
    using System.Collections.Generic;
    using Chutzpah.Facts.Properties;
    using Chutzpah.FileProcessors;
    using Chutzpah.FrameworkDefinitions;
    using Chutzpah.Models;
    using Moq;
    using Xunit;
    using Xunit.Extensions;

    public class JasmineDefinitionFacts
    {
        private class JasmineDefinitionCreator : Testable<JasmineDefinition>
        {
            public JasmineDefinitionCreator()
            {
            }
        }

        public class FileUsesFramework
        {
            public static IEnumerable<object[]> TestSuites
            {
                get
                {
                    return new object[][]
                    {
                        new object[] { Resources.JSSpecSuite },
                        new object[] { Resources.JsTestDriverSuite },
                        new object[] { Resources.QUnitSuite },
                        new object[] { Resources.YUITestSuite }
                    };
                }
            }

            [Fact]
            public void ReturnsTrue_WithJasmineSuiteAndDefinitiveDetection()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.JasmineSuite, false));
            }

            [Fact]
            public void ReturnsTrue_WithJasmineSuiteAndBestGuessDetection()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.JasmineSuite, true));
            }

            [Theory]
            [PropertyData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndDefinitiveDetection(string suite)
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, false));
            }

            [Theory]
            [PropertyData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndBestGuessDetection(string suite)
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, true));
            }
        }

        public class ReferenceIsDependency
        {
            [Fact]
            public void ReturnsTrue_GivenJasmineFile()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("jasmine.js"));
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("jasmine-html.js"));
            }

            [Fact]
            public void ReturnsFalse_GivenQUnitFile()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency("qunit.js"));
            }

            [Fact]
            public void ReturnsFalse_GivenEmptyOrNullString()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(string.Empty));
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(null));
            }
        }

        public class Process
        {
            [Fact]
            public void CallsDependency_GivenOneProcessor()
            {
                var creator = new JasmineDefinitionCreator();
                var processor = creator.Mock<IJasmineReferencedFileProcessor>();
                processor.Setup(x => x.Process(It.IsAny<ReferencedFile>()));
                creator.ClassUnderTest.Process(new ReferencedFile());

                processor.Verify(x => x.Process(It.IsAny<ReferencedFile>()));
            }

            [Fact]
            public void CallsAllDependencies_GivenMultipleProcessors()
            {
                var creator = new JasmineDefinitionCreator();
                var processor1 = new Mock<IJasmineReferencedFileProcessor>();
                var processor2 = new Mock<IJasmineReferencedFileProcessor>();
                processor1.Setup(x => x.Process(It.IsAny<ReferencedFile>()));
                processor2.Setup(x => x.Process(It.IsAny<ReferencedFile>()));
                creator.InjectArray<IJasmineReferencedFileProcessor>(new[] { processor1.Object, processor2.Object });

                creator.ClassUnderTest.Process(new ReferencedFile());

                processor1.Verify(x => x.Process(It.IsAny<ReferencedFile>()));
                processor2.Verify(x => x.Process(It.IsAny<ReferencedFile>()));
            }
        }

        public class GetFixtureNode
        {
            [Fact]
            public void ReturnsFixtureContent_GivenCustomHarness()
            {
                var creator = new JasmineDefinitionCreator();
                var expected = @"
<div>My Test Fixture <span>Content</span></div>
";

                var actual = creator.ClassUnderTest.GetFixtureContent(Resources.JasmineHarness);

                Assert.Equal(expected, actual);
            }

            [Fact]
            public void ReturnsEmpty_GivenInvalidHarness()
            {
                var creator = new JasmineDefinitionCreator();
                var harness = "I am not a valid test harness";

                var actual = creator.ClassUnderTest.GetFixtureContent(harness);

                Assert.Equal(string.Empty, actual);
            }
        }
    }
}
