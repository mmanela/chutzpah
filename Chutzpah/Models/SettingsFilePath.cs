namespace Chutzpah.Models
{
    public abstract class SettingsFilePath
    {
        /// <summary>
        /// The path of file/folder to include. This could be the path to one file or a folder.
        /// In the case of a folder all files found in the folder are used.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// A glob expression of the paths to include. This is usefull when you specify the path as a folder
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// A glob expression of the paths to exclude. This is usefull when you specify the path as a folder
        /// </summary>
        public string Exclude { get; set; }

        /// <summary>
        /// The settings file directory that this batch compile configuration came from
        /// </summary>
        public string SettingsFileDirectory { get; set; }
    }
}