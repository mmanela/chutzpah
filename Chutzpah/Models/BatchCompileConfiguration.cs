using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Chutzpah.Models
{
    public class BatchCompileConfiguration
    {
        public BatchCompileConfiguration()
        {
            Extensions = new List<string>();
        }

        /// <summary>
        /// The extension of the files which are getting compiled
        /// </summary>
        public ICollection<string> Extensions { get; set; }

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
        /// The command which Chutzpah executes to perform the batch compilation
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
    }
}