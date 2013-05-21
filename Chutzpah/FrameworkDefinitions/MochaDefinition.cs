using System.Collections.Generic;
using System.Text.RegularExpressions;
using Chutzpah.FileProcessors;

namespace Chutzpah.FrameworkDefinitions
{
    /// <summary>
    /// Definition that describes the QUnit framework.
    /// </summary>
    public class MochaDefinition : BaseFrameworkDefinition
    {
        private IEnumerable<IJasmineReferencedFileProcessor> fileProcessors;
        private IEnumerable<string> fileDependencies;

        /// <summary>
        /// Initializes a new instance of the MochaDefinition class.
        /// </summary>
        public MochaDefinition(IEnumerable<IJasmineReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;
            this.fileDependencies = new[]
                {
                    "Mocha\\mocha.css", 
                    "Mocha\\mocha.js"
                };
        }

        public override IEnumerable<string> FileDependencies
        {
            get { return fileDependencies; }
        }

        public override string TestHarness
        {
            get { return @"Mocha\mocha.html"; }
        }

        /// <summary>
        /// Gets a short, file system friendly key for the Mocha library.
        /// </summary>
        public override string FrameworkKey
        {
            get
            {
                return "mocha";
            }
        }

        /// <summary>
        /// Gets a list of file processors to call within the Process method.
        /// </summary>
        protected override IEnumerable<IReferencedFileProcessor> FileProcessors
        {
            get
            {
                return this.fileProcessors;
            }
        }
    }
}
