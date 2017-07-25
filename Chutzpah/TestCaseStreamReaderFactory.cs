using Chutzpah.Coverage;

namespace Chutzpah
{
    public class TestCaseStreamReaderFactory : ITestCaseStreamReaderFactory
    {
        public ITestCaseStreamReader Create()
        {
            return new TestCaseStreamStringReader();
        } 
    }
}