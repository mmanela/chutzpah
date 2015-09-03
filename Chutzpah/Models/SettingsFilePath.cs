using System.Collections.Generic;
namespace Chutzpah.Models
{
    public abstract class SettingsFilePath
    {

        public SettingsFilePath()
        {
            Includes = new List<string>();
            Excludes = new List<string>();
            ExpandReferenceComments = false;
        }

        /// <summary>
        /// The path of file/folder to include. This could be the path to one file or a folder.
        /// In the case of a folder all files found in the folder are used.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// A glob expression of the paths to include. This is usefull when you specify the path as a folder
        /// </summary>
        public string Include
        {
            set
            {
                Includes.Add(value);
            }
        }

        /// <summary>
        /// A glob expression of the paths to exclude. This is usefull when you specify the path as a folder
        /// </summary>
        public string Exclude
        {
            set
            {
                Excludes.Add(value);
            }
        }

        /// <summary>
        /// Glob expressions of the paths to include. 
        /// </summary>
        public IList<string> Includes { get; set; }

        /// <summary>
        /// Glob expressions of the paths to exclude. 
        /// </summary>
        public IList<string> Excludes { get; set; }



        /// <summary>
        /// The settings file directory that this batch compile configuration came from
        /// </summary>
        public string SettingsFileDirectory { get; set; }

        /// <summary>
        /// Determines if we should expand references to other files found as reference comments inside the text of the file
        /// </summary>
        public bool ExpandReferenceComments { get; set; }
    }
}