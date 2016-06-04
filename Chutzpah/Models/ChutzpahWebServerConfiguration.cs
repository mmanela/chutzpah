using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Chutzpah.Models
{
    public class ChutzpahWebServerConfiguration
    {

        /// <summary>
        /// Determines if the web server is enabled for thi chutzpah.json
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// The default port to use. If this port is taken Chutzpah will try incrementing until it finds an available one.
        /// If a parent settings file has already set this it cannot be overriden
        /// </summary>
        public int? DefaultPort { get; set; }

        /// <summary>
        /// The root path of the server. All file paths are relative to this and should be in a directory below or equal to this.
        /// Defaults to settings file path
        /// </summary>
        public string RootPath { get; internal set; }
    }   
}