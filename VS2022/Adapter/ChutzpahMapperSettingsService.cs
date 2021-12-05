using Chutzpah.VS.Common.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.ComponentModel.Composition;
using System.IO;
using System.Xml.XPath;

namespace Chutzpah.VS2022.TestAdapter
{
    [Export(typeof(IRunSettingsService))]
    [Export(typeof(IChutzpahSettingsMapper))]
    [SettingsName("ChutzpahAdapterSettings")]
    public class ChutzpahAdapterSettingsService : ChutzpahAdapterSettingsProvider, IRunSettingsService, IChutzpahSettingsMapper
    {
        public ChutzpahAdapterSettingsService() : base()
        {}

        public void MapSettings(ChutzpahUTESettings settings)
        {
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

        private string SerializeSettings()
        {
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, Settings);
            return stringWriter.ToString();
        }

    }
}