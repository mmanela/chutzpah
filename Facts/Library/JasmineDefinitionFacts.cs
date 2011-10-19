namespace Chutzpah.Facts.Library
{
    using Chutzpah.Facts.Properties;
    using Chutzpah.FrameworkDefinitions;
    using Xunit;

    public class JasmineDefinitionFacts
    {
        public class FileUsesFramework
        {
            [Fact]
            public void ReturnsFalse_GivenQUnitSuite()
            {
                var definition = new JasmineDefinition();
                Assert.False(definition.FileUsesFramework(Resources.QUnitSuite));
            }

            [Fact]
            public void ReturnsTrue_GivenJasmineSuite()
            {
                var definition = new JasmineDefinition();
                Assert.True(definition.FileUsesFramework(Resources.JasmineSuite));
            }
        }
    }
}
