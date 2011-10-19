namespace Chutzpah.Facts.Library
{
    using Chutzpah.Facts.Properties;
    using Chutzpah.FrameworkDefinitions;
    using Xunit;

    public class QUnitDefinitionFacts
    {
        public class FileUsesFramework
        {
            [Fact]
            public void GivenJasmineScript_ReturnsFalse()
            {
                var definition = new QUnitDefinition();
                Assert.False(definition.FileUsesFramework(Resources.JasmineSuite));
            }

            [Fact]
            public void GivenQUnitScript_ReturnsTrue()
            {
                var definition = new QUnitDefinition();
                Assert.True(definition.FileUsesFramework(Resources.QUnitSuite));
            }
        }
    }
}
