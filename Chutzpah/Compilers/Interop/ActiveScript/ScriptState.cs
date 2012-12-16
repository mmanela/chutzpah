namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Contains named constant values that specify the state of a scripting engine
    /// </summary>
    public enum ScriptState : uint
    {
        /// <summary>
        /// Script has just been created, but has not yet been initialized using an IPersist*
        /// interface and IActiveScript.SetScriptSite.
        /// </summary>
        Uninitialized = 0,

        /// <summary>
        /// Script can execute code, but is not yet sinking the events of objects added by
        /// the IActiveScript.AddNamedItem method.
        /// </summary>
        Started = 1,

        /// <summary>
        /// Script is loaded and connected for sinking events.
        /// </summary>
        Connected = 2,

        /// <summary>
        /// Script is loaded and has a run-time execution state, but is temporarily
        /// disconnected from sinking events.
        /// </summary>
        Disconnected = 3,

        /// <summary>
        /// Script has been closed. The scripting engine no longer works and returns errors
        /// for most methods.
        /// </summary>
        Closed = 4,

        /// <summary>
        /// Script has been initialized, but is not running (connecting to other objects or
        /// sinking events) or executing any code. Code can be queried for execution by
        /// calling the IActiveScriptParse.ParseScriptText method.
        /// </summary>
        Initialized = 5
    }
}