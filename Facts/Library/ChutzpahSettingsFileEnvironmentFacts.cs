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
            var environment = new ChutzpahSettingsFileEnvironment("path");
            environment.Path = @"C:\some\path";
            environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty("name1", "val1"));
            var environmentsWrapper = new ChutzpahSettingsFileEnvironments(new[] { environment });

            var matched = environmentsWrapper.GetSettingsFileEnvironment(@"c:\some\path\chutzpah.json");

            Assert.Equal(environment, matched);
        }

        [Fact]
        public void Will_normalize_slashes_and_casing()
        {
            var environment = new ChutzpahSettingsFileEnvironment("path");
            environment.Path = @"C:/Some/path";
            environment.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty("name1", "val1"));
            var environmentsWrapper = new ChutzpahSettingsFileEnvironments(new[] { environment });

            var matched = environmentsWrapper.GetSettingsFileEnvironment(@"c:\some\path\chutzpah.json");

            Assert.Equal(environment, matched);
        }

        [Fact]
        public void Will_choose_longest_matching_path()
        {
            var environment1 = new ChutzpahSettingsFileEnvironment("path");
            environment1.Path = @"C:\some\";
            environment1.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty("name1", "val1"));
            var environment2 = new ChutzpahSettingsFileEnvironment("path");
            environment2.Path = @"C:\some\path\";
            environment2.Properties.Add(new ChutzpahSettingsFileEnvironmentProperty( "name2", "val2" ));
            var environmentsWrapper = new ChutzpahSettingsFileEnvironments(new[] { environment1, environment2 });

            var matched = environmentsWrapper.GetSettingsFileEnvironment(@"c:\some\path\chutzpah.json");

            Assert.Equal(environment2, matched);
        }
    }
}
