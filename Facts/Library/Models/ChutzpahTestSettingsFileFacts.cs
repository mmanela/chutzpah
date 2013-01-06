using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Moq;
using Xunit;

namespace Chutzpah.Facts.Library.Models
{
    public class ChutzpahTestSettingsFileFacts
    {
        [Fact]
        public void Will_set_settings_file_directory()
        {
            var mockFileProbe = new Mock<IFileProbe>();
            var mockSerializer = new Mock<IJsonSerializer>();
            var settings = new ChutzpahTestSettingsFile();
            mockFileProbe.Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir\settingsFile.json");
            mockSerializer.Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            ChutzpahTestSettingsFile.Read("dir", mockFileProbe.Object, mockSerializer.Object);

            Assert.Equal(@"C:\settingsDir", settings.SettingsFileDirectory);
        }

        [Fact]
        public void Will_set_custom_harness_directory_based_relative_to_settings_file_directory()
        {
            var mockFileProbe = new Mock<IFileProbe>();
            var mockSerializer = new Mock<IJsonSerializer>();
            var settings = new ChutzpahTestSettingsFile { TestHarnessLocationMode = TestHarnessLocationMode.Custom, TestHarnessDirectory = "custom" };
            mockFileProbe.Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir2\settingsFile.json");
            mockFileProbe.Setup(x => x.FindFolderPath(@"C:\settingsDir2\custom")).Returns(@"customPath");
            mockSerializer.Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            ChutzpahTestSettingsFile.Read("dir2", mockFileProbe.Object, mockSerializer.Object);

            Assert.Equal(@"customPath", settings.TestHarnessDirectory);
        }

        [Fact]
        public void Will_get_cached_settings_given_same_starting_directory()
        {
            var mockFileProbe = new Mock<IFileProbe>();
            var mockSerializer = new Mock<IJsonSerializer>();
            var settings = new ChutzpahTestSettingsFile();
            mockFileProbe.Setup(x => x.FindTestSettingsFile("dir3")).Returns(@"C:\settingsDir3\settingsFile.json");
            mockSerializer.Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);
            ChutzpahTestSettingsFile.Read("dir3", mockFileProbe.Object, mockSerializer.Object);

            var cached = ChutzpahTestSettingsFile.Read("dir3", mockFileProbe.Object, mockSerializer.Object);

            Assert.Equal(@"C:\settingsDir3", cached.SettingsFileDirectory);
        }

        [Fact]
        public void Will_get_cached_settings_given_same_settings_directory()
        {
            var mockFileProbe = new Mock<IFileProbe>();
            var mockSerializer = new Mock<IJsonSerializer>();
            var settings = new ChutzpahTestSettingsFile();
            mockFileProbe.Setup(x => x.FindTestSettingsFile("dir4")).Returns(@"C:\settingsDir4\settingsFile.json");
            mockSerializer.Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);
            ChutzpahTestSettingsFile.Read("dir4", mockFileProbe.Object, mockSerializer.Object);

            var cached = ChutzpahTestSettingsFile.Read(@"C:\settingsDir4\", mockFileProbe.Object, mockSerializer.Object);

            Assert.Equal(@"C:\settingsDir4", cached.SettingsFileDirectory);
        }
    }
}
