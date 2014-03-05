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
    public class ChutzpahTestSettingsServiceFacts
    {
        private class TestableChutzpahTestSettingsService : Testable<ChutzpahTestSettingsService>
        {

        }

        [Fact]
        public void Will_set_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFile("dir");

            Assert.Equal(@"C:\settingsDir", settings.SettingsFileDirectory);
        }

        [Fact]
        public void Will_set_custom_harness_directory_based_relative_to_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile { TestHarnessLocationMode = TestHarnessLocationMode.Custom, TestHarnessDirectory = "custom" };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir2\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir2\custom")).Returns(@"customPath");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFile("dir2");

            Assert.Equal(@"customPath", settings.TestHarnessDirectory);
        }

        [Fact]
        public void Will_set_amdbasepath_based_relative_to_settings_file_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile { AMDBasePath = "custom" };
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile(It.IsAny<string>())).Returns(@"C:\settingsDir6\settingsFile.json");
            service.Mock<IFileProbe>().Setup(x => x.FindFolderPath(@"C:\settingsDir6\custom")).Returns(@"customPath");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);

            service.ClassUnderTest.FindSettingsFile("dir6");

            Assert.Equal(@"customPath", settings.AMDBasePath);
        }

        [Fact]
        public void Will_get_cached_settings_given_same_starting_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile("dir3")).Returns(@"C:\settingsDir3\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);
            service.ClassUnderTest.FindSettingsFile("dir3");

            var cached = service.ClassUnderTest.FindSettingsFile("dir3");

            Assert.Equal(@"C:\settingsDir3", cached.SettingsFileDirectory);
        }

        [Fact]
        public void Will_get_cached_settings_given_same_settings_directory()
        {
            var service = new TestableChutzpahTestSettingsService();
            var settings = new ChutzpahTestSettingsFile();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile("dir4")).Returns(@"C:\settingsDir4\settingsFile.json");
            service.Mock<IJsonSerializer>().Setup(x => x.DeserializeFromFile<ChutzpahTestSettingsFile>(It.IsAny<string>())).Returns(settings);
            service.ClassUnderTest.FindSettingsFile("dir4");

            var cached = service.ClassUnderTest.FindSettingsFile(@"C:\settingsDir4\");

            Assert.Equal(@"C:\settingsDir4", cached.SettingsFileDirectory);
        }

        [Fact]
        public void Will_cache_missing_default_settings_for_missing_settings_files()
        {
            var service = new TestableChutzpahTestSettingsService();
            service.Mock<IFileProbe>().Setup(x => x.FindTestSettingsFile("dir5")).Returns((string)null);
            service.ClassUnderTest.FindSettingsFile(@"dir5");

            var cached = service.ClassUnderTest.FindSettingsFile(@"dir5");

            service.Mock<IFileProbe>().Verify(x => x.FindTestSettingsFile("dir5"), Times.Once());
        }
    }
}
