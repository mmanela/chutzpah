using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;
using Xunit;

namespace Chutzpah.Facts.Library
{
    public class ChutzpahSettingsFileEnvironmentFacts
    {
        [Fact]
        public void Will_match_environment_given_chutzpah_json_path()
        {
            var environment = new ChutzpahSettingsFileEnvironment();
            environment.Path = @"C:\some\path";
            environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty { Name = "name1", Value = "val1" });
            var environmentsWrapper = new ChutzpahSettingsFileEnvironments(new[] { environment });

            var matched = environmentsWrapper.GetPropertiesForEnvironment(@"c:\some\path\chutzpah.json");

            Assert.Equal(environment.Properties, matched);
        }

        [Fact]
        public void Will_normalize_slashes_and_casing()
        {
            var environment = new ChutzpahSettingsFileEnvironment();
            environment.Path = @"C:/Some/path";
            environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty { Name = "name1", Value = "val1" });
            var environmentsWrapper = new ChutzpahSettingsFileEnvironments(new[] { environment });

            var matched = environmentsWrapper.GetPropertiesForEnvironment(@"c:\some\path\chutzpah.json");

            Assert.Equal(environment.Properties, matched);
        }

        [Fact]
        public void Will_choose_longest_matching_path()
        {
            var environment1 = new ChutzpahSettingsFileEnvironment();
            environment1.Path = @"C:\some\";
            environment1.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty { Name = "name1", Value = "val1" });
            var environment2 = new ChutzpahSettingsFileEnvironment();
            environment2.Path = @"C:\some\path\";
            environment2.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty { Name = "name2", Value = "val2" });
            var environmentsWrapper = new ChutzpahSettingsFileEnvironments(new[] { environment1, environment2 });

            var matched = environmentsWrapper.GetPropertiesForEnvironment(@"c:\some\path\chutzpah.json");

            Assert.Equal(environment2.Properties, matched);
        }
    }
}
