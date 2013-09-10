namespace Chutzpah.FrameworkDefinitions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Chutzpah.FileProcessors;

    /// <summary>
    /// Definition that describes the Mocha framework.
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
            this.fileDependencies = new []
                {
                    "mocha\\mocha.css", 
                    "mocha\\mocha.js", 
                };
        }

        /// <summary>
        /// Gets a list of file dependencies to bundle with the Mocha test harness.
        /// </summary>
        public override IEnumerable<string> FileDependencies
        {
            get
            {
                return this.fileDependencies;
            }
        }

        public override string TestHarness
        {
            get { return @"mocha\mocha.html"; }
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
