using Chutzpah.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Transformers
{
    public interface ITransformProcessor
    {
        void ProcessTransforms(IEnumerable<TestContext> testContexts, TestCaseSummary overallSummary);
    }
}
