using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Chutzpah.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Chutzpah.VS2012.TestAdapter
{
    public class ChutzpahAdapterSettings : TestRunSettings
    {
        public const string SettingsName = "ChutzpahAdapterSettings";

        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ChutzpahAdapterSettings));

        public ChutzpahAdapterSettings() : base(SettingsName)
        {
            TimeoutMilliseconds = null;
            TestingMode = TestingMode.JavaScript;
            MaxDegreeOfParallelism = 1;
        }

        /// <summary>
        /// How long to wait for a given test to finish before timing out? (Defaults to 5000 ms)
        /// </summary>
        public int? TimeoutMilliseconds { get; set; }

        /// <summary>
        /// Determines if we are testing JavaScript files (and creating harnesses for them), testing html test harnesses directly or both
        /// </summary>
        public TestingMode TestingMode { get; set; }

        /// <summary>
        /// Determines the maximum degree of paralleism Chutzpah should use
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }

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