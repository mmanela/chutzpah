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

    public class QUnitDefinitionFacts
    {
        private class QUnitDefinitionCreator : Testable<QUnitDefinition>
        {
            public QUnitDefinitionCreator()
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
                        new object[] { Resources.JasmineSuite },
                        new object[] { Resources.JSSpecSuite },
                        new object[] { Resources.JsTestDriverSuite },
                        new object[] { Resources.YUITestSuite }
                    };
                }
            }

            [Fact]
            public void ReturnsTrue_GivenQUnitSuiteAndDefinitiveDetection()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.QUnitSuite, false));
            }

            [Fact]
            public void ReturnsTrue_GivenQUnitSuiteAndBestGuessDetection()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.QUnitSuite, true));
            }

            [Theory]
            [PropertyData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndDefinitiveDetection(string suite)
            {
                var creator = new QUnitDefinitionCreator();
                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, false));
            }

            [Theory]
            [PropertyData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndBestGuessDetection(string suite)
            {
                var creator = new QUnitDefinitionCreator();
                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, true));
            }
        }

        public class ReferenceIsDependency
        {
            [Fact]
            public void ReturnsTrue_GivenQUnitFile()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("qunit.js"));
            }

            [Fact]
            public void ReturnsFalse_GivenJasmineFile()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency("jasmine.js"));
            }
        }

        public class Process
        {
            [Fact]
            public void CallsDependency_GivenOneProcessor()
            {
                var creator = new QUnitDefinitionCreator();
                var processor = creator.Mock<IQUnitReferencedFileProcessor>();
                processor.Setup(x => x.Process(It.IsAny<ReferencedFile>()));

                creator.ClassUnderTest.Process(new ReferencedFile());

                processor.Verify(x => x.Process(It.IsAny<ReferencedFile>()));
            }

            [Fact]
            public void CallsAllDependencies_GivenMultipleProcessors()
            {
                var creator = new QUnitDefinitionCreator();
                var processor1 = creator.Mock<IQUnitReferencedFileProcessor>();
                var processor2 = creator.Mock<IQUnitReferencedFileProcessor>();
                processor1.Setup(x => x.Process(It.IsAny<ReferencedFile>()));
                processor2.Setup(x => x.Process(It.IsAny<ReferencedFile>()));
                creator.InjectArray<IQUnitReferencedFileProcessor>(new[] { processor1.Object, processor2.Object });

                creator.ClassUnderTest.Process(new ReferencedFile());

                processor1.Verify(x => x.Process(It.IsAny<ReferencedFile>()));
                processor2.Verify(x => x.Process(It.IsAny<ReferencedFile>()));
            }
        }
    }
}
