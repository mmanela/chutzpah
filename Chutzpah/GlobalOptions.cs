using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah
{
    public sealed class GlobalOptions
    {
        private static readonly GlobalOptions instance = new GlobalOptions();
        private GlobalOptions()
        {
            CompilerCacheMaxSize = 1;
        }

        public static GlobalOptions Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Whether or not compiler-caching is enabled
        /// </summary>
        public bool EnableCompilerCache { get; set; }


        /// <summary>
        /// The path to the cachefile 
        /// </summary>
        public string CompilerCacheFile { get; set; }

        /// <summary>
        /// The maximum size of the created cachefile
        /// </summary>
        public int? CompilerCacheMaxSize { get; set; }
    }
}
