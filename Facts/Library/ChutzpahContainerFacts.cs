using Xunit;

namespace Chutzpah.Facts.Library
{
    public class ChutzpahContainerFacts
    {
        [Fact]
        public void ContainerIsValid()
        {
            ChutzpahContainer.Current.AssertConfigurationIsValid();
        }

        [Fact]
        public void WillGetNewTestRunnerEachTime()
        {
            var testRunner1 = TestRunner.Create();
            var testRunner2 = TestRunner.Create();

            Assert.NotEqual(testRunner1, testRunner2);

        }
    }

}
