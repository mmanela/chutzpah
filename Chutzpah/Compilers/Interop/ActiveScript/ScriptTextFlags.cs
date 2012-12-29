using System;

namespace Chutzpah.Compilers.ActiveScript
{
    [Flags]
    public enum ScriptTextFlags : uint
    {
        DelayExecution = 0x00000001,

        /// <summary>
        /// Indicates that the script text should be visible (and, therefore, callable by name) as a global
        /// method in the name space of the script.
        /// </summary>
        IsVisible = 0x00000002,

        /// <summary>
        /// If the distinction between a computational expression and a statement is important but
        /// syntactically ambiguous in the script language, this flag specifies that the scriptlet is to be
        /// interpreted as an expression, rather than as a statement or list of statements. By default,
        /// statements are assumed unless the correct choice can be determined from the syntax of the
        /// scriptlet text.
        /// </summary>
        IsExpression = 0x00000020,

        /// <summary>
        /// Indicates that the code added during this call should be saved if the scripting engine is saved
        /// (for example, through a call to IPersist*::Save), or if the scripting engine is reset by way of
        /// a transition back to the initialized state. For more information about this state, see Script
        /// Engine States.
        /// </summary>
        IsPersistent = 0x00000040,
        HostManagesSource = 0x00000080,
        IsCrossDomain = 0x00000100,
    }
}