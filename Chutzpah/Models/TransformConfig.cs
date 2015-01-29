using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Models
{
    /// <summary>
    /// Specifies a summary transform to be executed and its target output path.
    /// </summary>
    public class TransformConfig
    {
        /// <summary>
        /// Gets or sets the name of the transform to be executed.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the output path for the transform.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The settings file directory that this batch compile configuration came from
        /// </summary>
        public string SettingsFileDirectory { get; set; }
    }
}
