using System;
using Chutzpah.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Chutzpah.VS2012.TestAdapter
{
    public class DiscoveryCallback : RunnerCallback
    {
        private readonly IMessageLogger logger;
        private readonly ITestCaseDiscoverySink discoverySink;

        public DiscoveryCallback(IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            this.logger = logger;
            this.discoverySink = discoverySink;
        }

        public override void FileError(TestError error)
        {
            logger.SendMessage(TestMessageLevel.Error, GetFileErrorMessage(error));
        }

        public override void ExceptionThrown(Exception exception, string fileName)
        {
            logger.SendMessage(TestMessageLevel.Error, GetExceptionThrownMessage(exception, fileName));
        }

        public override void TestFinished(TestCase test)
        {
            var testCase = test.ToVsTestCase();
            discoverySink.SendTestCase(testCase);
        }

    }
}