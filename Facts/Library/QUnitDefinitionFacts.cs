namespace Chutzpah.Facts.Library
{
    using System.Collections.Generic;
    using Chutzpah.Facts.Properties;
    using Chutzpah.FrameworkDefinitions;
    using Xunit;
    using Xunit.Extensions;

    public class QUnitDefinitionFacts
    {
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
                var definition = new QUnitDefinition();
                Assert.True(definition.FileUsesFramework(Resources.QUnitSuite, false));
            }

            [Fact]
            public void ReturnsTrue_GivenQUnitSuiteAndBestGuessDetection()
            {
                var definition = new QUnitDefinition();
                Assert.True(definition.FileUsesFramework(Resources.QUnitSuite, true));
            }

            [Theory]
            [PropertyData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndDefinitiveDetection(string suite)
            {
                var definition = new QUnitDefinition();
                Assert.False(definition.FileUsesFramework(suite, false));
            }

            [Theory]
            [PropertyData("TestSuites")]
            public void ReturnsFalse_WithForeignSuiteAndBestGuessDetection(string suite)
            {
                var definition = new QUnitDefinition();
                Assert.False(definition.FileUsesFramework(suite, true));
            }
        }

        public class ReferenceIsDependency
        {
            [Fact]
            public void ReturnsTrue_GivenQUnitFile()
            {
                var definition = new QUnitDefinition();
                Assert.True(definition.ReferenceIsDependency("qunit.js"));
            }

            [Fact]
            public void ReturnsFalse_GivenJasmineFile()
            {
                var definition = new QUnitDefinition();
                Assert.False(definition.ReferenceIsDependency("jasmine.js"));
            }
        }
    }
}
