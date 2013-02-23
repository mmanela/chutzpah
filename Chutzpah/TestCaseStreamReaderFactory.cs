using Chutzpah.Coverage;

namespace Chutzpah
{
    public class TestCaseStreamReaderFactory : ITestCaseStreamReaderFactory
    {
        private ICoverageEngine coverageEngine;

        public TestCaseStreamReaderFactory(ICoverageEngine coverageEngine)
        {
            this.coverageEngine = coverageEngine;
        }

        public ITestCaseStreamReader Create()
        {
            return new TestCaseStreamReader(coverageEngine);
        } 
    }
}