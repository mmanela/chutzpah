using System;

namespace Chutzpah.Compilers.ActiveScript
{
    [Flags]
    public enum ScriptProcedureFlags : uint
    {
        /// <summary>
        /// Indicates that the code in pstrCode is an expression that represents the return value of the procedure.
        /// By default, the code can contain an expression, a list of statements, or anything else allowed in a
        /// procedure by the script language.
        /// </summary>
        IsExpression = 0x00000020,
        HostManagesSource = 0x00000080,

        /// <summary>
        /// Indicates that the this pointer is included in the scope of the procedure.
        /// </summary>
        ImplicitThis = 0x00000100,

        /// <summary>
        /// Indicates that the parents of the this pointer are included in the scope of the procedure.
        /// </summary>
        ImplicitParents = 0x00000200,
        IsCrossDomain = 0x00000400,
    }
}