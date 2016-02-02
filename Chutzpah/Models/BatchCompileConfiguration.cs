using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Chutzpah.Models
{
    public enum BatchCompileMode
    {
        External,
        Executable
    }


    public enum CompilePathType
    {
        File,
        Folder
    }

    public class CompilePathMap
    {
        /// <summary>
        /// The source file/directory 
        /// </summary>
        public string SourcePath { get; set; }

        [JsonIgnore]
        public bool SourcePathIsFile { get; set; }
        
        /// <summary>
        /// The file/directory that source file/directory is mapped to 
        /// Specifying a file OutputPath and a directory for SourcePath
        /// indicated the files are being concatentated into one large file
        /// </summary>
        public string OutputPath { get; set; }

        [JsonIgnore]
        public bool OutputPathIsFile { get; set; }

        /// <summary>
        /// The type (file or folder) that the output path refers to. If not specified 
        /// Chutzpah will try to take a best guess by assuming it is a file if it has a .js extension
        /// </summary>
        public CompilePathType? OutputPathType { get; set; }
    }

    public class BatchCompileConfiguration
    {
        public BatchCompileConfiguration()
        {
            Extensions = new List<string>();
            ExtensionsWithNoOutput = new List<string>();
            SkipIfUnchanged = true;
            Paths = new List<CompilePathMap>();
        }

        /// <summary>
        /// Determines the mode of the compile setting. By default it is to run the executable but if you set it to External
        /// then Chutzpah assumes some external force will compile and Chutzpah will just look for generated JS files
        /// </summary>
        public BatchCompileMode? Mode { get; set; }

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
        /// Deprecated in favor of Paths element
        /// The root directory where all the sources the command compiles are below.
        /// This lets Chutzpah know where in the out dir it should find each reference file
        /// </summary>
        public string SourceDirectory { get; set; }

        /// <summary>
        /// Deprecated in favor of Paths element
        /// The directory where the compiled files are output to
        /// </summary>
        public string OutDirectory { get; set; }

        /// <summary>
        /// The collection of path mapping from source directory/file to output directory/file
        /// </summary>
        public ICollection<CompilePathMap> Paths { get; set; }

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
        public bool? SkipIfUnchanged { get; set; }

        /// <summary>
        /// Configures whether .map files should be loaded (if available) to convert under-test JS
        /// line numbers to those of their original source files.
        /// </summary>
        public bool? UseSourceMaps { get; set; }

        /// <summary>
        /// Should Chutzpah ignore files it expects to find compiled. If set
        /// to true Chutzpah will log an error otherwise it will throw
        /// </summary>
        public bool? IgnoreMissingFiles{ get; set; }

        /// <summary>
        /// The settings file directory that this batch compile configuration came from
        /// </summary>
        [JsonIgnore]
        public string SettingsFileDirectory { get; set; }
    }
}