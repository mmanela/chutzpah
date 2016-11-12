using Chutzpah.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Transformers
{
    public class SummaryTransformerProvider : ISummaryTransformerProvider
    {
        public IEnumerable<SummaryTransformer> GetTransformers(IFileSystemWrapper fileSystem)
        {
            return new SummaryTransformer[]
            {
                new JUnitXmlTransformer(fileSystem),
                new LcovTransformer(fileSystem),
                new TrxXmlTransformer(fileSystem),
                new NUnit2XmlTransformer(fileSystem),
                new CoverageHtmlTransformer(fileSystem),
                new CoverageJsonTransformer(fileSystem),
                new EmmaXmlTransformer(fileSystem),
                new JacocoTransformer(fileSystem),
            };
        }
    }
}
