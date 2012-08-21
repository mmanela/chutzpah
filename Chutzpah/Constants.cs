namespace Chutzpah
{
    public static class Constants
    {
        // Default time in milliseconds to wait for new test results. If we don't hear anything
        // from phantom after this amount of time abort
        public const int DefaultTestFileTimeout = 5000;

        // Default of how many files to open during test file discovery
        public const int DefaultFileSeachLimit = 300;

        // Format for temporary files Chutzpah creates that should be ignored in source controler.
        // These get generated when Chutzpah needs to generate a file in place like when it needs to convert 
        // Coffee script to JS
        public const string ChutzpahTemporaryFileFormat = "_Chutzpah.{0}";

    }
}