using System;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;

namespace Chutzpah.Compilers.ActiveScript
{
    [Serializable]
    public class ActiveScriptException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveScriptException"/> class.
        /// </summary>
        public ActiveScriptException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveScriptException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ActiveScriptException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveScriptException"/> class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public ActiveScriptException(Exception innerException)
            : base(null, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveScriptException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ActiveScriptException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveScriptException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="info"/> parameter is null.
        ///   </exception>
        ///   
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">
        /// The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0).
        ///   </exception>
        protected ActiveScriptException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static ActiveScriptException Create(IActiveScriptError error)
        {
            string source = "";
            uint sourceContext = 0;
            uint lineNumber = 0;
            int characterPosition = 0;
            EXCEPINFO excepInfo;

            string message = "";

            try
            {
                error.GetSourceLineText(out source);
            }
            catch
            {
            }

            try
            {
                error.GetSourcePosition(out sourceContext, out lineNumber, out characterPosition);
                ++lineNumber;
                ++characterPosition;
            }
            catch
            {
            }

            try
            {
                error.GetExceptionInfo(out excepInfo);
                message = string.Format(
                    "Error in [{1}]:\n{0}\nat line {4}({5})\nError Code: {2} (0x{2:X8})\nError WCode: {3}",
                    /* 0 */ excepInfo.bstrDescription,
                    /* 1 */ excepInfo.bstrSource,
                    /* 2 */ excepInfo.scode,
                    /* 3 */ excepInfo.wCode,
                    /* 4 */ lineNumber,
                    /* 5 */ characterPosition,
                    /* 6 */ source);
            }
            catch
            {
            }

            return new ActiveScriptException(message)
                {
                    LineContent = source,
                    SourceContext = sourceContext,
                    LineNumber = lineNumber,
                    Column = characterPosition,
                };
        }

        /// <summary>
        /// Gets or sets the application specific source context.
        /// </summary>
        public uint SourceContext { get; set; }

        /// <summary>
        /// Gets or sets the line number on which the error occurred.
        /// </summary>
        public uint LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the column on which the error occurred..
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the content of the line on which the error occurred..
        /// </summary>
        public string LineContent { get; set; }
    }
}