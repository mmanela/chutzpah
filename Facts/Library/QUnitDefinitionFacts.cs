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
            public void ReturnsFalse_GivenJasmineSuite()
            {
                var definition = new QUnitDefinition();
                Assert.False(definition.FileUsesFramework(Resources.JasmineSuite));
            }

            [Fact]
            public void ReturnsTrue_GivenQUnitSuite()
            {
                var definition = new QUnitDefinition();
                Assert.True(definition.FileUsesFramework(Resources.QUnitSuite));
            }
        }
    }
}
