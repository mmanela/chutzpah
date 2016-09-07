using System.Collections.Generic;
using Chutzpah.Facts.Properties;
using Chutzpah.FileProcessors;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Chutzpah.Facts.Library
{

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

                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.JasmineSuite, false, PathType.JavaScript));
            }

            [Fact]
            public void ReturnsTrue_WithJasmineSuiteAndBestGuessDetection()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.True(creator.ClassUnderTest.FileUsesFramework(Resources.JasmineSuite, true, PathType.JavaScript));
            }

            [Theory]
            [MemberData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndDefinitiveDetection(string suite)
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, false, PathType.JavaScript));
            }

            [Theory]
            [MemberData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndBestGuessDetection(string suite)
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.FileUsesFramework(suite, true, PathType.JavaScript));
            }
        }

        public class ReferenceIsDependency
        {
            [Fact]
            public void ReturnsTrue_GivenJasmineFile_version2()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("jasmine.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("jasmine-html.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("boot.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
            }

            [Fact]
            public void ReturnsTrue_GivenJasmineFile_version1()
            {
                var settings = new ChutzpahTestSettingsFile {FrameworkVersion = "1"};
                var creator = new JasmineDefinitionCreator();

                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("jasmine.js", settings));
                Assert.True(creator.ClassUnderTest.ReferenceIsDependency("jasmine-html.js", settings));
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency("boot.js", settings));
            }

            [Fact]
            public void ReturnsFalse_GivenQUnitFile()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency("qunit.js", new ChutzpahTestSettingsFile().InheritFromDefault()));
            }

            [Fact]
            public void ReturnsFalse_GivenEmptyOrNullString()
            {
                var creator = new JasmineDefinitionCreator();

                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(string.Empty, new ChutzpahTestSettingsFile().InheritFromDefault()));
                Assert.False(creator.ClassUnderTest.ReferenceIsDependency(null, new ChutzpahTestSettingsFile().InheritFromDefault()));
            }
        }

        public class Process
        {
            [Fact]
            public void CallsDependency_GivenOneProcessor()
            {
                var creator = new JasmineDefinitionCreator();
                var processor = creator.Mock<IJasmineReferencedFileProcessor>();
                creator.ClassUnderTest.Process(new ReferencedFile(), "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }

            [Fact]
            public void CallsAllDependencies_GivenMultipleProcessors()
            {
                var creator = new JasmineDefinitionCreator();
                var processor1 = new Mock<IJasmineReferencedFileProcessor>();
                var processor2 = new Mock<IJasmineReferencedFileProcessor>();
                creator.InjectArray<IJasmineReferencedFileProcessor>(new[] { processor1.Object, processor2.Object });

                creator.ClassUnderTest.Process(new ReferencedFile(), "", new ChutzpahTestSettingsFile().InheritFromDefault());

                processor1.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
                processor2.Verify(x => x.Process(It.IsAny<IFrameworkDefinition>(), It.IsAny<ReferencedFile>(), It.IsAny<string>(), It.IsAny<ChutzpahTestSettingsFile>()));
            }
        }

    }
}
