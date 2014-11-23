using Chutzpah.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Transformers
{
    public interface ISummaryTransformerProvider
    {
        IEnumerable<SummaryTransformer> GetTransformers(IFileSystemWrapper fileSystem);
    }
}
