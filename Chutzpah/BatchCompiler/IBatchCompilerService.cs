using System.Collections;
using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah.BatchProcessor
{
    public interface IBatchCompilerService
    {
        void Compile(IEnumerable<TestContext> testContexts, ITestMethodRunnerCallback callback = null);
    }
}
