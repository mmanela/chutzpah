using Chutzpah.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Chutzpah.VS2022.TestAdapter
{

    public class ChutzpahAdapterSettings : TestRunSettings
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ChutzpahAdapterSettings));

        private ChutzpahSettingsFileEnvironments environmentsWrapper = null;

        public ChutzpahAdapterSettings() : base(AdapterConstants.SettingsName)
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount;
            EnabledTracing = false;
            ChutzpahSettingsFileEnvironments = new Collection<ChutzpahSettingsFileEnvironment>();
        }

        /// <summary>
        /// Determines the maximum degree of paralleism Chutzpah should use
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Determines if chutzpah tracing is enabled
        /// </summary>
        public bool EnabledTracing { get; set; }

        /// <summary>
        /// Whether or not to launch the tests in the default browser
        /// </summary>
        public bool OpenInBrowser { get; set; }

        public Collection<ChutzpahSettingsFileEnvironment> ChutzpahSettingsFileEnvironments { get; set; }

        [XmlIgnore]
        public ChutzpahSettingsFileEnvironments ChutzpahSettingsFileEnvironmentsWrapper
        {
            get
            {
                if (environmentsWrapper == null)
                {
                    environmentsWrapper = new ChutzpahSettingsFileEnvironments(ChutzpahSettingsFileEnvironments);
                }

                return environmentsWrapper;
            }
        }

        public override XmlElement ToXml()
        {
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, this);
            var xml = stringWriter.ToString();
            var document = new XmlDocument();
            document.LoadXml(xml);
            return document.DocumentElement;
        }

    }
}