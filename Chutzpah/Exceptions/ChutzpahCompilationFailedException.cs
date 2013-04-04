using System;

namespace Chutzpah.Exceptions
{
    public class ChutzpahCompilationFailedException : ChutzpahException
    {
        public ChutzpahCompilationFailedException(string message) : base(message)
        {
            
        }

        public string SourceFile { get; set; }

        public override string ToString()
        {
            var ret = Message;
            if (SourceFile != null)
            {
                ret += string.Format(" [in file {0}]", SourceFile);
            }
            return ret;
        }
    }
}