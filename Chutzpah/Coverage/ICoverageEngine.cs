using System.Collections.Generic;
using Chutzpah.Models;

namespace Chutzpah.Coverage
{
    public interface ICoverageEngine
    {
        /// <summary>
        /// File name pattern that, if set, a file must match to be instrumented. Pattern matching
        /// is done with the <c>PathMatchSpec</c> Windows function.
        /// </summary>
        string IncludePattern { get; set; }

        /// <summary>
        /// File name pattern that, if set, a file must NOT match to be instrumented. Pattern matching 
        /// is done with the <c>PathMatchSpec</c> Windows function.
        /// </summary>
        string ExcludePattern { get; set; }

        bool CanUse(IList<string> messages);

        void Instrument(string stagingFolder, IList<ReferencedFile> referencedFiles, IList<string> temporaryFiles, out string coverageHelperPath);
    }
}
