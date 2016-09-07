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
                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.QUnitSuite, false, PathType.JavaScript));
            }

            [Fact]
            public void ReturnsTrue_GivenQUnitSuiteAndBestGuessDetection()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.QUnitSuite, true, PathType.JavaScript));
            }

            [Theory]
            [MemberData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndDefinitiveDetection(string suite)
            {
                var creator = new QUnitDefinitionCreator();
                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, false, PathType.JavaScript));
            }

            [Theory]
            [MemberData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndBestGuessDetection(string suite)
            {
                var creator = new QUnitDefinitionCreator();
                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, true, PathType.JavaScript));
            }
        }

        public class ReferenceIsDependency
        {
            [Fact]
            public void ReturnsTrue_GivenQUnitFile()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("qunit.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
            }

            [Fact]
            public void ReturnsFalse_GivenJasmineFile()
            {
                var creator = new QUnitDefinitionCreator();
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency("jasmine.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
            }

            [Fact]
            public void ReturnsFalse_GivenEmptyOrNullString()
            {
                var creator = new QUnitDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(string.Empty, new ChutzpahTestSettingsFile().InheritFromDefault()));
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(null, new ChutzpahTestSettingsFile().InheritFromDefault()));
            }
        }

        public class Process
        {
            [Fact]
            public void CallsDependency_GivenOneProcessor()
            {
                var creator = new QUnitDefinitionCreator();
                var processor = creator.Mock<IQUnitReferencedFileProcessor>();

                creator.ClassUnderTest.Process(new ReferencedFile(), "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }

            [Fact]
            public void CallsAllDependencies_GivenMultipleProcessors()
            {
                var creator = new QUnitDefinitionCreator();
                var processor1 = creator.Mock<IQUnitReferencedFileProcessor>();
                var processor2 = creator.Mock<IQUnitReferencedFileProcessor>();
                creator.InjectArray<IQUnitReferencedFileProcessor>(new[] { processor1.Object, processor2.Object });

                creator.ClassUnderTest.Process(new ReferencedFile(), "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor1.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
                processor2.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }
        }
    }
}
