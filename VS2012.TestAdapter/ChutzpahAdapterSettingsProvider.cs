using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.ComponentModel.Composition;
using System.Xml;
using System.Xml.Serialization;

namespace Chutzpah.VS2022.TestAdapter
{
    [Export(typeof(ISettingsProvider))]
    [SettingsName("ChutzpahAdapterSettings")]
    public class ChutzpahAdapterSettingsProvider : ISettingsProvider
    {
        protected readonly XmlSerializer serializer;

        // Locally remmember settings
        public ChutzpahAdapterSettings Settings { get; private set; }

        public string Name { get; private set; }

        public ChutzpahAdapterSettingsProvider()
        {
            Name = AdapterConstants.SettingsName;
            Settings = new ChutzpahAdapterSettings();
            serializer = new XmlSerializer(typeof(ChutzpahAdapterSettings));
        }

        public void Load(XmlReader reader)
        {
            ValidateArg.NotNull(reader, "reader");

            if (reader.Read() && reader.Name.Equals(AdapterConstants.SettingsName))
            {
                Settings = serializer.Deserialize(reader) as ChutzpahAdapterSettings;
            }
        }

    }
}
