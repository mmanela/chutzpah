namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Contains named constant values that specify the state of a thread in a scripting
    /// engine. This enumeration is used by the IActiveScript::GetScriptThreadState method.
    /// </summary>
    public enum ScriptThreadState : uint
    {
        /// <summary>
        /// Specified thread is not currently servicing a scripted event, processing
        /// immediately executed script text, or running a script macro.
        /// </summary>
        NotInScriipt = 0,

        /// <summary>
        /// Specified thread is actively servicing a scripted event, processing
        /// immediately executed script text, or running a script macro.
        /// </summary>
        Running = 1,
    }
}