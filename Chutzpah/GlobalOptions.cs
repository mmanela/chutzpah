namespace Chutzpah
{
    public sealed class GlobalOptions
    {
        private static readonly GlobalOptions instance = new GlobalOptions();

        private GlobalOptions()
        {
            CompilerCacheFile = null;
            CompilerCacheFileMaxSizeBytes = Constants.DefaultCompilerCacheFileMaxSize;
        }

        public static GlobalOptions Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The path to the cachefile 
        /// </summary>
        public string CompilerCacheFile { get; set; }

        /// <summary>
        /// Max size for cache file
        /// </summary>
        public int CompilerCacheFileMaxSizeBytes { get; set; }
    }
}