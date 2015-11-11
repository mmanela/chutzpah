using Chutzpah.Models;
using Chutzpah.Wrappers;
using System;
using System.Text;
using Chutzpah.Coverage;

namespace Chutzpah.Transformers
{
    public class CoverageJsonTransformer : SummaryTransformer
    {
        public override string Name
        {
            get { return Constants.DefaultCoverageJsonTransform; }
        }

        public override string Description
        {
            get { return "Outputs the default Chutzpah coverage JSON"; }
        }

        public CoverageJsonTransformer(IFileSystemWrapper fileSystem)
            : base(fileSystem)
        {

        }

        public override void Transform(TestCaseSummary testFileSummary, string outFile)
        {
            if (testFileSummary == null)
            {
                throw new ArgumentNullException("testFileSummary");
            }
             
            if (string.IsNullOrEmpty(outFile))
            {
                throw new ArgumentNullException("outFile");
            }
            
            if (testFileSummary.CoverageObject == null)
            {
                return;
            }

            CoverageOutputGenerator.WriteJsonFile(outFile, testFileSummary.CoverageObject);
        }


        // We are overwritting the default Transform above so we do not need this method
        public override string Transform(TestCaseSummary testFileSummary)
        {
            throw new NotImplementedException();
        }

    }
}
