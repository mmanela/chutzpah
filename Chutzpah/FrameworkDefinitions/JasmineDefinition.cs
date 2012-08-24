namespace Chutzpah.FrameworkDefinitions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Chutzpah.FileProcessors;

    /// <summary>
    /// Definition that describes the Jasmine framework.
    /// </summary>
    public class JasmineDefinition : BaseFrameworkDefinition
    {
        private IEnumerable<IJasmineReferencedFileProcessor> fileProcessors;
        private IEnumerable<string> fileDependencies;

        /// <summary>
        /// Initializes a new instance of the JasmineDefinition class.
        /// </summary>
        public JasmineDefinition(IEnumerable<IJasmineReferencedFileProcessor> fileProcessors)
        {
            this.fileProcessors = fileProcessors;
            this.fileDependencies = base.FileDependencies.Concat(new []
                {
                    "jasmine\\jasmine-html.js", 
                    "jasmine\\jasmine_favicon.png"
                });
        }

        /// <summary>
        /// Gets a list of file dependencies to bundle with the Jasmine test harness.
        /// </summary>
        public override IEnumerable<string> FileDependencies
        {
            get
            {
                return this.fileDependencies;
            }
        }

        /// <summary>
        /// Gets a short, file system friendly key for the Jasmine library.
        /// </summary>
        protected override string FrameworkKey
        {
            get
            {
                return "jasmine";
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable Jasmine file in a JavaScript file.
        /// </summary>
        protected override Regex FrameworkSignatureJavaScript
        {
            get
            {
                return RegexPatterns.JasmineTestRegexJavaScript;
            }
        }

        /// <summary>
        /// Gets a regular expression pattern to match a testable Jasmine file in a CoffeeScript file.
        /// </summary>
        protected override Regex FrameworkSignatureCoffeeScript
        {
            get
            {
                return RegexPatterns.JasmineTestRegexCoffeeScript;
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
