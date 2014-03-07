using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class BatchCompileConfiguration
    {
        public BatchCompileConfiguration()
        {
            Extensions = new List<string>();
            ExtensionsWithNoOutput = new List<string>();
            SkipIfUnchanged = true;
        }

        /// <summary>
        /// The extension of the files which are getting compiled
        /// </summary>
        public ICollection<string> Extensions { get; set; }


        /// <summary>
        /// The extensions of files which take part in compile but have no build output. This is used for cases like TypeScript declaration files.
        /// They have .d.ts extension and are part of compilation but have no output. You must tell Chutzpah about these if you want the SkipIfUnchanged setting to work.
        /// Otherwise Chutzpah will things these are missing output. If not using that setting then you don't need to specify this
        /// </summary>
        public ICollection<string> ExtensionsWithNoOutput { get; set; }

        /// <summary>
        /// This is the working directory of the process which executes the commad. This will default to the 
        /// directory of the settings file
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The root directory where all the sources the command compiles are below.
        /// This lets Chutzpah know where in the out dir it should find each reference file
        /// </summary>
        /// 
        public string SourceDirectory { get; set; }
        /// <summary>
        /// The directory where the compiled files are output to
        /// </summary>
        public string OutDirectory { get; set; }

        /// <summary>
        /// The full path to an executable which Chutzpah executes to perform the batch compilation
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// The arguments to pass to the command
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// How long to wait for compile to finish in milliseconds. Defaults to 5 minutes
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Skips the execution if all files Chutzpah knows about are older than all of the output files
        /// This is defaulted to true but if you hit issues since it is possible chutzpah might not know about all the files your compilation
        /// code is using then you can turn this off. Ideally you can tell Chutzpah about these and then set this to be more performant
        /// </summary>
        public bool SkipIfUnchanged { get; set; }
    }
}