namespace Chutzpah
{
    public class TestCaseStreamReaderFactory : ITestCaseStreamReaderFactory
    {
        public ITestCaseStreamReader Create()
        {
            var reader = ChutzpahContainer.Current.GetInstance<ITestCaseStreamReader>();
            return reader;
        } 
    }
}