namespace Chutzpah.Models
{
    public class SettingsFileReference : SettingsFilePath
    {
        public SettingsFileReference()
        {
            IncludeInTestHarness = true;
            TemplateOptions = new TemplateOptions();
        }

        /// <summary>
        /// Indicate if this file should be included in the test harness. When running Chutzpah with
        /// TestHarnessReferenceMode set to AMD by default Chutzpah will not add references from reference comments
        /// but will add referenced from reference settings unless you set this to false
        /// </summary>
        public bool IncludeInTestHarness { get; set; }

        /// <summary>
        /// Marks this reference as a test framework dependency. This means the files are referenced towards the top of the harness
        /// along with the test framework reference 
        /// </summary>
        public bool IsTestFrameworkFile { get; set; }

        /// <summary>
        /// Additional options to specify if this reference is an HTML file
        /// </summary>
        public TemplateOptions TemplateOptions { get; set; }
    }
}