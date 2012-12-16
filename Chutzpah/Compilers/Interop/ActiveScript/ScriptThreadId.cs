namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Signifies a special thread or class of threads.
    /// </summary>
    public enum ScriptThreadId : uint
    {
        /// <summary>
        /// The currently executing thread.
        /// </summary>
        Current = 0xFFFFFFFD,

        /// <summary>
        /// The base thread; that is, the thread in which the scripting engine was instantiated.
        /// </summary>
        Base = 0xFFFFFFFE,

        /// <summary>
        /// All threads.
        /// </summary>
        All = 0xFFFFFFFF
    }
}