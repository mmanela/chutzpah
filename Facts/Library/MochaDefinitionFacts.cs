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

    public class MochaDefinitionFacts
    {
        private class MochaDefinitionCreator : Testable<MochaDefinition>
        {
            public MochaDefinitionCreator()
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
            public void ReturnsTrue_WithMochaSuiteAndDefinitiveDetection()
            {
                var creator = new MochaDefinitionCreator();

                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.MochaSuite, false, PathType.JavaScript));
            }

            [Fact]
            public void ReturnsFalse_WithMochaSuiteAndBestGuessDetection()
            {
                var creator = new MochaDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(Resources.MochaSuite, true, PathType.JavaScript));
            }

            [Theory]
            [MemberData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndDefinitiveDetection(string suite)
            {
                var creator = new MochaDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, false, PathType.JavaScript));
            }

            [Theory]
            [MemberData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndBestGuessDetection(string suite)
            {
                var creator = new MochaDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, true, PathType.JavaScript));
            }
        }

        public class ReferenceIsDependency
        {
            [Fact]
            public void ReturnsTrue_GivenMochaFile()
            {
                var creator = new MochaDefinitionCreator();

                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("mocha.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("mocha.css", new ChutzpahTestSettingsFile().InheritFromDefault()));
            }

            [Fact]
            public void ReturnsFalse_GivenQUnitFile()
            {
                var creator = new MochaDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency("qunit.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
            }

            [Fact]
            public void ReturnsFalse_GivenEmptyOrNullString()
            {
                var creator = new MochaDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(string.Empty, new ChutzpahTestSettingsFile().InheritFromDefault()));
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(null, new ChutzpahTestSettingsFile().InheritFromDefault()));
            }
        }

        public class Process
        {
            [Fact]
            public void CallsDependency_GivenOneProcessor()
            {
                var creator = new MochaDefinitionCreator();
                var processor = creator.Mock<IMochaReferencedFileProcessor>();
                creator.ClassUnderTest.Process(new ReferencedFile(), "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }

            [Fact]
            public void CallsAllDependencies_GivenMultipleProcessors()
            {
                var creator = new MochaDefinitionCreator();
                var processor1 = new Mock<IMochaReferencedFileProcessor>();
                var processor2 = new Mock<IMochaReferencedFileProcessor>();
                creator.InjectArray<IMochaReferencedFileProcessor>(new[] { processor1.Object, processor2.Object });

                creator.ClassUnderTest.Process(new ReferencedFile(), "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor1.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
                processor2.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }
        }

    }
}
