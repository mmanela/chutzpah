using System;

namespace Chutzpah.Exceptions
{
    public class ChutzpahCompilationFailedException : ChutzpahException
    {
        public ChutzpahCompilationFailedException(string message) : base(message)
        {
        }

        public ChutzpahCompilationFailedException(string message, string settingsFile)
            : this(message)
        {
            SettingsFile = settingsFile;
        }

        public ChutzpahCompilationFailedException(string message, string settingsFile, Exception e)
            : base(message, e)
        {
            SettingsFile = settingsFile;
        }

        public string SourceFile { get; set; }

        public string SettingsFile { get; set; }

        public override string ToString()
        {
            var ret = Message;
            if (SourceFile != null)
            {
                ret = string.Format("{0}: {1}", SourceFile, ret);
            }
            return ret;
        }
    }
}