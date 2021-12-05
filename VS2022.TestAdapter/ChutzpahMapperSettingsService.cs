using System.ComponentModel.Composition;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Chutzpah.VS.Common.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Chutzpah.VS2022.TestAdapter
{
    [Export(typeof(ISettingsProvider))]
    [Export(typeof(IRunSettingsService))]
    [Export(typeof(IChutzpahSettingsMapper))]
    [SettingsName("ChutzpahAdapterSettings")]
    public class ChutzpahAdapterSettingsService : IRunSettingsService, ISettingsProvider, IChutzpahSettingsMapper
    {
        private readonly XmlSerializer serializer;

        // Locally remmember settings
        public ChutzpahAdapterSettings Settings { get; private set; }
        
        public string Name { get; private set; }

        public ChutzpahAdapterSettingsService()
        {
            Name = AdapterConstants.SettingsName;
            Settings = new ChutzpahAdapterSettings();
            serializer = new XmlSerializer(typeof(ChutzpahAdapterSettings));
        }

        public void MapSettings(ChutzpahUTESettings settings)
        {
            Settings.TestingMode = settings.TestingMode;
            Settings.TimeoutMilliseconds = settings.TimeoutMilliseconds;
            Settings.MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism;
            Settings.EnabledTracing = settings.EnabledTracing;
        }

        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, ILogger log)
        {
            ValidateArg.NotNull(inputRunSettingDocument, "inputRunSettingDocument");
            ValidateArg.NotNull(configurationInfo, "configurationInfo");

            var navigator = inputRunSettingDocument.CreateNavigator();
            if(navigator.MoveToChild("RunSettings",""))
            {
                if (navigator.MoveToChild(AdapterConstants.SettingsName, ""))
                {
                    navigator.DeleteSelf();
                }

                navigator.AppendChild(SerializeSettings());

            }

            navigator.MoveToRoot();
            return navigator;
        }

        /// <summary>
        /// Load the chutzpah adapter settings from the reader
        /// </summary>
        /// <param name="reader"></param>
        public void Load(XmlReader reader)
        {
            ValidateArg.NotNull(reader, "reader");

            if (reader.Read() && reader.Name.Equals(AdapterConstants.SettingsName))
            {
                Settings = serializer.Deserialize(reader) as ChutzpahAdapterSettings;
            }
        }


        private string SerializeSettings()
        {
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, Settings);
            return stringWriter.ToString();
        }

    }
}