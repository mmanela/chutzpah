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

        protected CommandLine(string[] args)
        {
            for (var i = args.Length - 1; i >= 0; i--)
                arguments.Push(args[i]);

            UnmatchedArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Files = new List<string>();
            SettingsFileEnvironments = new ChutzpahSettingsFileEnvironments();
            TeamCity = Environment.GetEnvironmentVariable(TeamcityProjectName) != null;
            Parse();
        }


        public bool ShowFailureReport { get; set; }

        public bool Discovery { get; set; }

        public bool Trace { get; set; }

        public bool FailOnError { get; set; }

        public bool Debug { get; protected set; }

        public bool Silent { get; protected set; }
        public bool NoLogo { get; protected set; }

        public bool OpenInBrowser { get; protected set; }

        public int? TimeOutMilliseconds { get; protected set; }

        public int Parallelism { get; protected set; }

        public bool TeamCity { get; protected set; }

        public bool Wait { get; protected set; }

        public IList<string> Files { get; set; }

        public ChutzpahSettingsFileEnvironments SettingsFileEnvironments { get; set; }

        public IDictionary<string,string> UnmatchedArguments { get; set; }

        public bool VsOutput { get; protected set; }

        public bool Coverage { get; protected set; }

        public string CoverageIncludePatterns { get; protected set; }

        public string CoverageExcludePatterns { get; protected set; }

        public string CoverageIgnorePatterns { get; protected set; }

        public string BrowserName { get; protected set; }

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
                else if (optionName == "/discovery")
                {
                    GuardNoOptionValue(option);
                    Discovery = true;
                }
                else if (optionName == "/debug")
                {
                    GuardNoOptionValue(option);
                    Debug = true;
                }
                else if (optionName == "/trace")
                {
                    GuardNoOptionValue(option);
                    Trace = true;
                }
                else if (optionName == "/failonerror" || optionName == "/failonscripterror")
                {
                    GuardNoOptionValue(option);
                    FailOnError = true;
                }
                else if (optionName == "/openinbrowser")
                {
                    AddBrowserName(option.Value);
                    OpenInBrowser = true;
                }
                else if (optionName == "/silent")
                {
                    GuardNoOptionValue(option);
                    Silent = true;
                }
                else if (optionName == "/nologo")
                {
                    GuardNoOptionValue(option);
                    NoLogo = true;
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
                else if (optionName == "/coverageignores")
                {
                    AddCoverageIgnoreOption(option.Value);
                }
                else if (optionName == "/showfailurereport")
                {
                    GuardNoOptionValue(option);
                    ShowFailureReport = true;
                }
                else if (optionName == "/settingsfileenvironment")
                {
                    AddSettingsFileEnvironment(option.Value);
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

        private void AddCoverageIgnoreOption(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "invalid or missing argument for /coverageIgnores.  Expecting a list of comma separated file name patterns");
            }
            CoverageIgnorePatterns = value;
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

        private void AddBrowserName(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Equals("ie", StringComparison.InvariantCultureIgnoreCase) ||
                    value.Equals("chrome", StringComparison.InvariantCultureIgnoreCase) ||
                    value.Equals("firefox", StringComparison.InvariantCultureIgnoreCase))
                {
                    BrowserName = value;
                }
                else
                {
                    throw new ArgumentException("invalid browser name, expecting either ie, chrome or firefox");
                }
            }
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

        
        private void AddSettingsFileEnvironment(string environmentStr)
        {   
            if (string.IsNullOrEmpty(environmentStr))
            {
                throw new ArgumentException(
                    "missing argument for /settingsFileEnvironment.  Expecting the settings file path and at least one property (e.g. settingsFilePath;prop1=val1;prop2=val2)");
            }

            var envParts = environmentStr.Split(new []{";"},StringSplitOptions.RemoveEmptyEntries);
            if(envParts.Length <2)
            {  
                throw new ArgumentException(
                    "invalid argument for /settingsFileEnvironment.  Expecting the settings file path and at least one property (e.g. settingsFilePath;prop1=val1;prop2=val2)");
            }

            var path = envParts[0];
            var environment = new ChutzpahSettingsFileEnvironment(path);
            for (var i = 1; i < envParts.Length; i++)
            {
                var propParts = envParts[i].Split('=');
                var name = propParts[0];
                var value = propParts.Length > 1 ? propParts[1] : "";
                environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty(name, value));
            }

            SettingsFileEnvironments.AddEnvironment(environment);
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