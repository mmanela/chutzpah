using System;
using System.Runtime.InteropServices.ComTypes;

namespace Chutzpah.Compilers.ActiveScript
{
    public sealed class ActiveScriptParseWrapper : IActiveScriptParseWrapper
    {
        private const string NeitherValidMessage =
            "The parser you passed implements neither IActiveScriptParse32 nor IActiveScriptParse64";

        private readonly IActiveScriptParse32 _parse32;
        private readonly IActiveScriptParse64 _parse64;

        private EXCEPINFO _exceptionInfo;

        /// <summary>
        /// Gets the last COM exception.
        /// </summary>
        public EXCEPINFO LastException
        {
            get { return _exceptionInfo; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveScriptParseWrapper"/> class.
        /// </summary>
        /// <param name="parser">The parser.  Must implement IActiveScriptParse32 or IActiveScriptParse64.</param>
        public ActiveScriptParseWrapper(object parser)
        {
            _parse32 = parser as IActiveScriptParse32;
            _parse64 = parser as IActiveScriptParse64;
        }

        /// <summary>
        /// Initializes the scripting engine.
        /// </summary>
        public void InitNew()
        {
            if (_parse32 != null)
                _parse32.InitNew();
            else if (_parse64 != null)
                _parse64.InitNew();
            else throw new NotImplementedException(NeitherValidMessage);
        }

        /// <summary>
        /// Adds a code scriptlet to the script. This method is used in environments where the
        /// persistent state of the script is intertwined with the host document and the host
        /// is responsible for restoring the script, rather than through an IPersist* interface.
        /// The primary examples are HTML scripting languages that allow scriptlets of code
        /// embedded in the HTML document to be attached to intrinsic events (for instance,
        /// ONCLICK="button1.text='Exit'").
        /// </summary>
        /// <param name="defaultName">The default name to associate with the scriptlet. If the
        /// scriptlet does not contain naming information (as in the ONCLICK example above),
        /// this name will be used to identify the scriptlet. If this parameter is NULL, the
        /// scripting engine manufactures a unique name, if necessary.</param>
        /// <param name="code">The scriptlet text to add. The interpretation of this string
        /// depends on the scripting language.</param>
        /// <param name="itemName">The item name associated with this scriptlet. This parameter,
        /// in addition to pstrSubItemName, identifies the object for which the scriptlet is
        /// an event handler.</param>
        /// <param name="subItemName">The name of a subobject of the named item with which this
        /// scriptlet is associated; this name must be found in the named item's type
        /// information. This parameter is NULL if the scriptlet is to be associated with the
        /// named item instead of a subitem. This parameter, in addition to pstrItemName,
        /// identifies the specific object for which the scriptlet is an event handler.</param>
        /// <param name="eventName">The name of the event for which the scriptlet is an event
        /// handler.</param>
        /// <param name="delimiter">The end-of-scriptlet delimiter. When the pstrCode parameter
        /// is parsed from a stream of text, the host typically uses a delimiter, such as two
        /// single quotation marks (''), to detect the end of the scriptlet. This parameter
        /// specifies the delimiter that the host used, allowing the scripting engine to
        /// provide some conditional primitive preprocessing (for example, replacing a single
        /// quotation mark ['] with two single quotation marks for use as a delimiter).
        /// Exactly how (and if) the scripting engine makes use of this information depends
        /// on the scripting engine. Set this parameter to NULL if the host did not use a
        /// delimiter to mark the end of the scriptlet.</param>
        /// <param name="sourceContextCookie">Application-defined value that is used for
        /// debugging purposes.</param>
        /// <param name="startingLineNumber">Zero-based value that specifies which line the
        /// parsing will begin at.</param>
        /// <param name="flags">Flags associated with the scriptlet.</param>
        /// <returns>
        /// Actual name used to identify the scriptlet. This is to be in
        /// order of preference: a name explicitly specified in the scriptlet text, the
        /// default name provided in pstrDefaultName, or a unique name synthesized by the
        /// scripting engine.
        /// </returns>
        public string AddScriptlet(
            string defaultName,
            string code,
            string itemName,
            string subItemName,
            string eventName,
            string delimiter,
            IntPtr sourceContextCookie,
            uint startingLineNumber,
            ScriptTextFlags flags)
        {
            string name;

            if (_parse32 != null)
                _parse32.AddScriptlet(
                    defaultName,
                    code,
                    itemName,
                    subItemName,
                    eventName,
                    delimiter,
                    sourceContextCookie,
                    startingLineNumber,
                    flags,
                    out name,
                    out _exceptionInfo);
            else if (_parse64 != null)
                _parse64.AddScriptlet(
                    defaultName,
                    code,
                    itemName,
                    subItemName,
                    eventName,
                    delimiter,
                    sourceContextCookie,
                    startingLineNumber,
                    flags,
                    out name,
                    out _exceptionInfo);
            else throw new NotImplementedException(NeitherValidMessage);

            return name;
        }

        /// <summary>
        /// Parses the given code scriptlet, adding declarations into the namespace and
        /// evaluating code as appropriate.
        /// </summary>
        /// <param name="code">The scriptlet text to evaluate. The interpretation of this
        /// string depends on the scripting language.</param>
        /// <param name="itemName">The item name that gives the context in which the
        /// scriptlet is to be evaluated. If this parameter is NULL, the code is evaluated
        /// in the scripting engine's global context.</param>
        /// <param name="context">The context object. This object is reserved for use in a
        /// debugging environment, where such a context may be provided by the debugger to
        /// represent an active run-time context. If this parameter is NULL, the engine
        /// uses pstrItemName to identify the context.</param>
        /// <param name="delimiter">The end-of-scriptlet delimiter. When pstrCode is parsed
        /// from a stream of text, the host typically uses a delimiter, such as two single
        /// quotation marks (''), to detect the end of the scriptlet. This parameter specifies
        /// the delimiter that the host used, allowing the scripting engine to provide some
        /// conditional primitive preprocessing (for example, replacing a single quotation
        /// mark ['] with two single quotation marks for use as a delimiter). Exactly how
        /// (and if) the scripting engine makes use of this information depends on the
        /// scripting engine. Set this parameter to NULL if the host did not use a delimiter
        /// to mark the end of the scriptlet.</param>
        /// <param name="sourceContextCookie">Application-defined value that is used for
        /// debugging purposes.</param>
        /// <param name="startingLineNumber">Zero-based value that specifies which line the
        /// parsing will begin at.</param>
        /// <param name="flags">Flags associated with the scriptlet.</param>
        /// <returns>
        /// The results of scriptlet processing, or NULL if the caller
        /// expects no result (that is, the SCRIPTTEXT_ISEXPRESSION value is not set).
        /// </returns>
        public object ParseScriptText(
            string code,
            string itemName,
            object context,
            string delimiter,
            IntPtr sourceContextCookie,
            uint startingLineNumber,
            ScriptTextFlags flags)
        {
            object result;

            if (_parse32 != null)
                _parse32.ParseScriptText(
                    code,
                    itemName,
                    context,
                    delimiter,
                    sourceContextCookie,
                    startingLineNumber,
                    flags,
                    out result,
                    out _exceptionInfo);
            else if (_parse64 != null)
                _parse64.ParseScriptText(
                    code,
                    itemName,
                    context,
                    delimiter,
                    sourceContextCookie,
                    startingLineNumber,
                    flags,
                    out result,
                    out _exceptionInfo);
            else throw new NotImplementedException(NeitherValidMessage);

            return result;
        }
    }
}