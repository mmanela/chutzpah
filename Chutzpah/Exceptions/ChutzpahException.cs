using System;

namespace Chutzpah.Exceptions
{
    public class ChutzpahException : Exception
    {
        public ChutzpahException()
        {
        }

        public ChutzpahException(string message)
            : base(message)
        {

        }


        public ChutzpahException(string message, Exception e)
            : base(message, e)
        {

        }
    }
}