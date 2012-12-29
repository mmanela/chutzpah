using System.Runtime.InteropServices;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// If the Windows Script engine allows the source code text for procedures to be added to the script,
    /// it implements the IActiveScriptParseProcedure interface. For interpreted scripting languages that
    /// have no independent authoring environment, such as VBScript, this provides an alternate mechanism
    /// (other than IActiveScriptParse or IPersist*) to add script procedures to the namespace.
    /// </summary>
    [Guid("C64713B6-E029-4CC5-9200-438B72890B6A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActiveScriptParseProcedure64
    {
        /// <summary>
        /// Parses the given code procedure and adds the procedure to the name space.
        /// </summary>
        /// <param name="code">The procedure text to evaluate. The interpretation of this string depends on
        /// the scripting language.</param>
        /// <param name="formalParameters">Formal parameter names for the procedure. The parameter names
        /// must be separated with the appropriate delimiters for the scripting engine. The names should not
        /// be enclosed in parentheses.</param>
        /// <param name="procedureName">The procedure name to be parsed.</param>
        /// <param name="itemName">The item name that gives the context in which the procedure is to be
        /// evaluated. If this parameter is NULL, the code is evaluated in the scripting engine's global
        /// context.</param>
        /// <param name="context">The context object. This object is reserved for use in a debugging
        /// environment, where such a context may be provided by the debugger to represent an active run-time
        /// context. If this parameter is NULL, the engine uses pstrItemName to identify the context.</param>
        /// <param name="delimeter">The end-of-procedure delimiter. When pstrCode is parsed from a stream of
        /// text, the host typically uses a delimiter, such as two single quotation marks (''), to detect the
        /// end of the procedure. This parameter specifies the delimiter that the host used, allowing the
        /// scripting engine to provide some conditional primitive preprocessing (for example, replacing a
        /// single quotation mark ['] with two single quotation marks for use as a delimiter). Exactly how
        /// (and if) the scripting engine makes use of this information depends on the scripting engine.
        /// Set this parameter to NULL if the host did not use a delimiter to mark the end of the procedure.</param>
        /// <param name="sourceContextCookie">Application-defined value that is used for debugging purposes.</param>
        /// <param name="startingLineNumber">Zero-based value that specifies which line the parsing will begin at.</param>
        /// <param name="flags">Flags associated with the procedure.</param>
        /// <param name="dispatch">The object containing the script's global methods and properties. If the
        /// scripting engine does not support such an object, NULL is returned.</param>
        void ParseProcedureText(
            [MarshalAs(UnmanagedType.LPWStr)] string code,
            [MarshalAs(UnmanagedType.LPWStr)] string formalParameters,
            [MarshalAs(UnmanagedType.LPWStr)] string procedureName,
            [MarshalAs(UnmanagedType.LPWStr)] string itemName,
            [MarshalAs(UnmanagedType.IUnknown)] object context,
            [MarshalAs(UnmanagedType.LPWStr)] string delimeter,
            ulong sourceContextCookie,
            uint startingLineNumber,
            ScriptProcedureFlags flags,
            [MarshalAs(UnmanagedType.IDispatch)] out object dispatch);
    }
}