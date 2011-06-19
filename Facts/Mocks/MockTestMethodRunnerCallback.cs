using Chutzpah.Models;
using Moq;

namespace Chutzpah.Facts.Mocks
{
    public class MockTestMethodRunnerCallback : Mock<ITestMethodRunnerCallback>
    {
        public MockTestMethodRunnerCallback()
        {
            Setup(x => x.FileStart(It.IsAny<string>())).Returns(true);
            Setup(x => x.FileFinished(It.IsAny<string>(), It.IsAny<TestResultsSummary>())).Returns(true);
        }
    }
}