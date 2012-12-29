using System;
using System.Runtime.InteropServices;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// If the Windows Script engine allows raw text code scriptlets to be added to the script
    /// or allows expression text to be evaluated at run time, it implements the
    /// IActiveScriptParse interface. For interpreted scripting languages that have no
    /// independent authoring environment, such as VBScript, this provides an alternate
    /// mechanism (other than IPersist*) to get script code into the scripting engine, and
    /// to attach script fragments to various object events.
    /// </summary>
    /// <remarks>
    /// Before the scripting engine can be used, one of the following methods must be called
    /// : IPersist*::Load, IPersist*::InitNew, or IActiveScriptParse::InitNew. The semantics
    /// of this method are identical to IPersistStreamInit::InitNew, in that this method tells
    /// the scripting engine to initialize itself. Note that it is not valid to call both
    /// IPersist*::InitNew or IActiveScriptParse::InitNew and IPersist*::Load, nor is it valid
    /// to call IPersist*::InitNew, IActiveScriptParse::InitNew, or IPersist*::Load more
    /// than once.
    /// </remarks>
    [ComVisible(false)]
    public interface IActiveScriptParseWrapper
    {
        /// <summary>
        /// Initializes the scripting engine.
        /// </summary>
        void InitNew();

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
        /// <returns>Actual name used to identify the scriptlet. This is to be in
        /// order of preference: a name explicitly specified in the scriptlet text, the
        /// default name provided in pstrDefaultName, or a unique name synthesized by the
        /// scripting engine.</returns>
        string AddScriptlet(
            string defaultName,
            string code,
            string itemName,
            string subItemName,
            string eventName,
            string delimiter,
            IntPtr sourceContextCookie,
            uint startingLineNumber,
            ScriptTextFlags flags);

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
        /// <returns>The results of scriptlet processing, or NULL if the caller
        /// expects no result (that is, the SCRIPTTEXT_ISEXPRESSION value is not set).</returns>
        object ParseScriptText(
            string code,
            string itemName,
            object context,
            string delimiter,
            IntPtr sourceContextCookie,
            uint startingLineNumber,
            ScriptTextFlags flags);
    }
}