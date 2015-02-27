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
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [XmlType("Environment")]
    public class ChutzpahSettingsFileEnvironment
    {
        public ChutzpahSettingsFileEnvironment()
        {
            Properties = new Collection<ChutzpahSettingsFileEnvironmentProperty>();
        }

        [XmlAttribute]
        public string Path { get; set; }

        public Collection<ChutzpahSettingsFileEnvironmentProperty> Properties { get; set; }
    }


    public class ChutzpahSettingsFileEnvironments
    {
        private ICollection<ChutzpahSettingsFileEnvironment> environments;

        public ChutzpahSettingsFileEnvironments()
        {
            environments = new List<ChutzpahSettingsFileEnvironment>();
        }

        public ChutzpahSettingsFileEnvironments(ICollection<ChutzpahSettingsFileEnvironment> environments)
        {
            if (environments == null)
            {
                throw new ArgumentNullException("environments");
            }

            this.environments = environments;

            foreach (var environment in environments)
            {
                // Normalize all paths
                environment.Path = FileProbe.NormalizeFilePath(environment.Path).TrimEnd('\\');
            }
        }

        public IEnumerable<ChutzpahSettingsFileEnvironmentProperty> GetPropertiesForEnvironment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Enumerable.Empty<ChutzpahSettingsFileEnvironmentProperty>();
            }

            path = FileProbe.NormalizeFilePath(path).TrimEnd('\\');


            // Find the longest path that matches the chutzpah.json file
            var matchedEnvironment = (  from environment in environments
                                        where path.StartsWith(environment.Path, StringComparison.OrdinalIgnoreCase)
                                        orderby environment.Path.Length descending
                                        select environment).FirstOrDefault();



            if (matchedEnvironment == null)
            {
                return Enumerable.Empty<ChutzpahSettingsFileEnvironmentProperty>();
            }
            else
            {
                return matchedEnvironment.Properties;
            }
        }

    }

}
