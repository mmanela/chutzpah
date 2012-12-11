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
            CompilerCacheFile = null;
        }

        public static GlobalOptions Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// The path to the cachefile 
        /// </summary>
        public string CompilerCacheFile { get; set; }

    }
}
