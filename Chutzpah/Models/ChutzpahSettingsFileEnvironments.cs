using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Chutzpah.Models
{
    [XmlType("Property")]
    public class ChutzpahSettingsFileEnvironmentProperty
    {
        public ChutzpahSettingsFileEnvironmentProperty()
        {

        }

        public ChutzpahSettingsFileEnvironmentProperty(string name, string value)
        {
            Name = name;
            Value = value ?? "";
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    [XmlType("Environment")]
    public class ChutzpahSettingsFileEnvironment
    {
        public ChutzpahSettingsFileEnvironment()
        {

        }

        public ChutzpahSettingsFileEnvironment(string path)
        {
            Path = path;
            Properties = new Collection<ChutzpahSettingsFileEnvironmentProperty>();
        }

        [XmlAttribute]
        public string Path { get; set; }

        public Collection<ChutzpahSettingsFileEnvironmentProperty> Properties { get; set; }
    }


    public class ChutzpahSettingsFileEnvironments
    {
        private Dictionary<string, ChutzpahSettingsFileEnvironment> environmentMap;

        public int Count
        {
            get
            {
                return environmentMap.Count;
            }
        }

        public ChutzpahSettingsFileEnvironments()
        {
            environmentMap = new Dictionary<string, ChutzpahSettingsFileEnvironment>(StringComparer.OrdinalIgnoreCase);
        }

        public ChutzpahSettingsFileEnvironments(ICollection<ChutzpahSettingsFileEnvironment> environments)
            : this()
        {
            if (environments == null)
            {
                throw new ArgumentNullException("environments");
            }

            foreach (var environment in environments)
            {
                AddEnvironment(environment);
            }
        }

        public void AddEnvironment(ChutzpahSettingsFileEnvironment environment)
        {
            environment.Path = ProcessEnvironmentPath(environment.Path);

            if (!environmentMap.ContainsKey(environment.Path))
            {
                environmentMap[environment.Path] = environment;
            }
        }


        public void RemoveEnvironment(string path)
        {
            path = ProcessEnvironmentPath(path);
            if (environmentMap.ContainsKey(path))
            {
                environmentMap.Remove(path);
            }

        }

        private static string ProcessEnvironmentPath(string path)
        {
            path = UrlBuilder.NormalizeFilePath(path).TrimEnd('\\', ' ');
            if (path.EndsWith(Constants.SettingsFileName, StringComparison.OrdinalIgnoreCase))
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        public ChutzpahSettingsFileEnvironment GetSettingsFileEnvironment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            path = ProcessEnvironmentPath(path);


            // Find the longest path that matches the chutzpah.json file
            var matchedEnvironment = (from environment in environmentMap.Values
                                      where path.StartsWith(environment.Path, StringComparison.OrdinalIgnoreCase)
                                      orderby environment.Path.Length descending
                                      select environment).FirstOrDefault();

            return matchedEnvironment;

        }

    }

}
