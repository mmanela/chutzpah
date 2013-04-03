using System;
using System.Collections.Generic;
using System.Globalization;
using Chutzpah.Models;

namespace Chutzpah
{
    public class CommandLine
    {
        private const string TeamcityProjectName = "TEAMCITY_PROJECT_NAME";

        private readonly Stack<string> arguments = new Stack<string>();
        private TestingMode testMode = TestingMode.All;

        protected CommandLine(string[] args)
        {
            for (var i = args.Length - 1; i >= 0; i--)
                arguments.Push(args[i]);

            UnmatchedArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Files = new List<string>();
            TeamCity = Environment.GetEnvironmentVariable(TeamcityProjectName) != null;
            Parse();
        }

        public bool FailOnScriptError { get; set; }

        public bool Debug { get; protected set; }

        public bool Silent { get; protected set; }

        public bool OpenInBrowser { get; protected set; }

        public int? TimeOutMilliseconds { get; protected set; }

        public int Parallelism { get; protected set; }

        public bool TeamCity { get; protected set; }

        public bool Wait { get; protected set; }

        public IList<string> Files { get; set; }

        public IDictionary<string,string> UnmatchedArguments { get; set; }

        public bool VsOutput { get; protected set; }

        public bool Coverage { get; protected set; }
        public TestingMode TestMode
        {
            get { return testMode; }
            protected set { testMode = value; }
        }

        public string CompilerCacheFile { get; protected set; }

        public string CoverageIncludePatterns { get; protected set; }
        public int? CompilerCacheFileMaxSizeMb { get; protected set; }

        public string CoverageExcludePatterns { get; protected set; }

        private static void GuardNoOptionValue(KeyValuePair<string, string> option)
        {
            if (option.Value != null)
                throw new ArgumentException(String.Format("unknown command line option: {0}", option.Value));
        }

        public static CommandLine Parse(string[] args)
        {
            return new CommandLine(args);
        }

        protected void Parse()
        {
            if (!arguments.Peek().StartsWith("/"))
            {
                var fileName = arguments.Pop();
                AddFileOption(fileName);
            }

            while (arguments.Count > 0)
            {
                KeyValuePair<string, string> option = PopOption(arguments);
                string optionName = option.Key.ToLowerInvariant();

                if (!optionName.StartsWith("/"))
                    throw new ArgumentException(String.Format("unknown command line option: {0}", option.Key));

                if (optionName == "/wait")
                {
                    GuardNoOptionValue(option);
                    Wait = true;
                }
                else if (optionName == "/debug")
                {
                    GuardNoOptionValue(option);
                    Debug = true;
                }
                else if (optionName == "/failonscripterror")
                {
                    GuardNoOptionValue(option);
                    FailOnScriptError = true;
                }
                else if (optionName == "/openinbrowser")
                {
                    GuardNoOptionValue(option);
                    OpenInBrowser = true;
                }
                else if (optionName == "/silent")
                {
                    GuardNoOptionValue(option);
                    Silent = true;
                }
                else if (optionName == "/teamcity")
                {
                    GuardNoOptionValue(option);
                    TeamCity = true;
                }
                else if (optionName == "/timeoutmilliseconds")
                {
                    AddTimeoutOption(option.Value);
                }
                else if (optionName == "/parallelism")
                {
                    AddParallelismOption(option.Value);
                }
                else if (optionName == "/file" || optionName == "/path")
                {
                    AddFileOption(option.Value);
                }
                else if (optionName == "/vsoutput")
                {
                    GuardNoOptionValue(option);
                    VsOutput = true;
                }
                else if (optionName == "/coverage")
                {
                    GuardNoOptionValue(option);
                    Coverage = true;
                }
                else if (optionName == "/coverageincludes")
                {
                    AddCoverageIncludeOption(option.Value);
                }
                else if (optionName == "/coverageexcludes")
                {
                    AddCoverageExcludeOption(option.Value);
                }
                else if (optionName == "/compilercachefile")
                {
                    SetCompilerCacheFile(option.Value);
                }
                else if (optionName == "/compilercachesize")
                {
                    SetCompilerCacheMaxSize(option.Value);
                }
                else if (optionName == "/testmode")
                {
                    TestingMode resultMode;
                    if(Enum.TryParse(option.Value, true, out resultMode))
                    {
                        TestMode = resultMode; 
                    }

                }
                else
                {
                    if (!optionName.StartsWith("/"))
                        throw new ArgumentException(String.Format("unknown command line option: {0}", option.Key));
                    else
                    {
                        UnmatchedArguments[optionName.Trim('/')] = option.Value;
                    }
                }
            }
        }

        private void AddCoverageIncludeOption(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "invalid or missing argument for /coverageIncludes.  Expecting a list of comma separated file name patterns");
            }
            CoverageIncludePatterns = value;
        }

        private void AddCoverageExcludeOption(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "invalid or missing argument for /coverageExcludes.  Expecting a list of comma separated file name patterns");
            }
            CoverageExcludePatterns = value;
        }

        private void SetCompilerCacheMaxSize(string value)
        {
            int maxSize;
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out maxSize) || maxSize < 0)
            {
                throw new ArgumentException(
                    "invalid or missing argument for /compilercachemaxsize.  Expecting a postivie integer");
            }

            CompilerCacheFileMaxSizeMb = maxSize;
        }

        private void SetCompilerCacheFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentException(
                    "missing argument for /compilercachefile.  Expecting a file path for the cache.");
            }
            CompilerCacheFile = file;
        }

        private void AddParallelismOption(string value)
        {
            int parallelism;
            if (string.IsNullOrEmpty(value))
            {
                // If no parallelism is specified, use CPU-count + 1
                parallelism = Environment.ProcessorCount + 1;
            }
            else if (!int.TryParse(value, out parallelism) || parallelism < 0)
            {
                throw new ArgumentException(
                    "invalid argument for /parallelism.  Expecting a optional positive integer");
            }
            
            

            Parallelism = parallelism;

        }

        private void AddTimeoutOption(string value)
        {
            int timeout;
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out timeout) || timeout < 0)
            {
                throw new ArgumentException(
                    "invalid or missing argument for /timeoutmilliseconds.  Expecting a postivie integer");
            }

            TimeOutMilliseconds = timeout;
        }

        private void AddFileOption(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentException(
                    "missing argument for /file.  Expecting a file path to a test file (e.g. test.html)");
            }

            Files.Add(file);
        }

        private static KeyValuePair<string, string> PopOption(Stack<string> arguments)
        {
            string option = arguments.Pop();
            string value = null;

            if (arguments.Count > 0 && !arguments.Peek().StartsWith("/"))
                value = arguments.Pop();

            return new KeyValuePair<string, string>(option, value);
        }
    }
}