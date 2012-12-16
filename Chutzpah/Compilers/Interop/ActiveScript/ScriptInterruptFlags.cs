using System;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Thread interruption options.
    /// </summary>
    [Flags]
    public enum ScriptInterruptFlags : uint
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// If supported, enter the scripting engine's debugger at the current script execution point.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// If supported by the scripting engine's language, let the script handle the exception.
        /// Otherwise, the script method is aborted and the error code is returned to the caller; that
        /// is, the event source or macro invoker.
        /// </summary>
        RaiseException = 2,
    }
}