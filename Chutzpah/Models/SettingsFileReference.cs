namespace Chutzpah.Models
{
    public class SettingsFileReference
    {

        /// <summary>
        /// The path of file/folder to include. This could be the path to one file or a folder.
        /// In the case of a folder all files found in the folder are used.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Force this file to always be included in the test harness. When running Chutzpah with
        /// TestHarnessReferenceMode set to AMD by default Chutzpah will not add references (besides ones for the test framework)
        /// into the test harness. If you have a file you want to always be included in the test harness set this bit.
        /// </summary>
        public bool AlwaysIncludeInTestHarness { get; set; }

        /// <summary>
        /// Marks this reference as a test framework dependency. This means the files are referenced towards the top of the harness
        /// along with the test framework reference 
        /// </summary>
        public bool IsTestFrameworkDependency { get; set; }

        /// <summary>
        /// A glob expression of the paths to include. This is usefull when you specify the path as a folder
        /// </summary>
        public string Include { get; set; }


        /// <summary>
        /// A glob expression of the paths to exclude. This is usefull when you specify the path as a folder
        /// </summary>
        public string Exclude { get; set; }
    }
}