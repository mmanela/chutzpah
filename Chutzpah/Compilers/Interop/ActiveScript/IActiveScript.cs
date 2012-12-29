using System;
using System.Runtime.InteropServices;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Provides the methods necessary to initialize the scripting engine. The scripting engine must
    /// implement the IActiveScript interface.
    /// </summary>
    [Guid("BB1A2AE1-A4F9-11cf-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActiveScript
    {
        /// <summary>
        /// Informs the scripting engine of the IActiveScriptSite interface site provided by the host.
        /// Call this method before any other IActiveScript interface methods is used.
        /// </summary>
        /// <param name="scriptSite">The host-supplied script site to be associated with this instance
        /// of the scripting engine. The site must be uniquely assigned to this scripting engine
        /// instance; it cannot be shared with other scripting engines.</param>
        void SetScriptSite(IActiveScriptSite scriptSite);

        /// <summary>
        /// Retrieves the site object associated with the Windows Script engine.
        /// </summary>
        /// <param name="iid">Identifier of the requested interface.</param>
        /// <param name="siteObject">The host's site object.</param>
        void GetScriptSite(Guid iid, out IActiveScriptSite siteObject);

        /// <summary>
        /// Puts the scripting engine into the given state. This method can be called from non-base
        /// threads without resulting in a non-base callout to host objects or to the IActiveScriptSite
        /// interface.
        /// </summary>
        /// <param name="scriptState">Sets the scripting engine to the given state.</param>
        void SetScriptState(ScriptState scriptState);

        /// <summary>
        /// Retrieves the current state of the scripting engine. This method can be called from
        /// non-base threads without resulting in a non-base callout to host objects or to the
        /// IActiveScriptSite interface.
        /// </summary>
        /// <param name="scriptState">The value indicates the current state of the scripting engine
        /// associated with the calling thread.</param>
        void GetScriptState(out ScriptState scriptState);

        /// <summary>
        /// Causes the scripting engine to abandon any currently loaded script, lose its state, and
        /// release any interface pointers it has to other objects, thus entering a closed state.
        /// Event sinks, immediately executed script text, and macro invocations that are already in
        /// progress are completed before the state changes (use IActiveScript::InterruptScriptThread to
        /// cancel a running script thread). This method must be called by the creating host before the
        /// interface is released to prevent circular reference problems.
        /// </summary>
        void Close();

        /// <summary>
        /// Adds the name of a root-level item to the scripting engine's name space. A root-level item
        /// is an object with properties and methods, an event source, or all three.
        /// </summary>
        /// <param name="name">The name of the item as viewed from the script. The name must be unique
        /// and persistable.</param>
        /// <param name="itemFlags">Flags associated with an item.</param>
        void AddNamedItem([MarshalAs(UnmanagedType.LPWStr)] string name, ScriptItemFlags itemFlags);

        /// <summary>
        /// Adds a type library to the name space for the script. This is similar to the #include
        /// directive in C/C++. It allows a set of predefined items such as class definitions, typedefs,
        /// and named constants to be added to the run-time environment available to the script.
        /// </summary>
        /// <param name="clsId">CLSID of the type library to add.</param>
        /// <param name="majorVersion">Major version number.</param>
        /// <param name="minorVersion">Minor version number.</param>
        /// <param name="typeLibFlags">Option flags.</param>
        void AddTypeLib(Guid clsId, uint majorVersion, uint minorVersion, ScriptTypeLibFlags typeLibFlags);

        /// <summary>
        /// Retrieves the IDispatch interface for the methods and properties associated with the
        /// currently running script.
        /// </summary>
        /// <param name="itemName">The name of the item for which the caller needs the associated
        /// dispatch object. If this parameter is NULL, the dispatch object contains as its members
        /// all of the global methods and properties defined by the script. Through the IDispatch
        /// interface and the associated ITypeInfo interface, the host can invoke script methods
        /// or view and modify script variables.</param>
        /// <param name="dispatch">The object associated with the script's global methods and
        /// properties. If the scripting engine does not support such an object, NULL is returned.</param>
        void GetScriptDispatch(
            [MarshalAs(UnmanagedType.LPWStr)] string itemName,
            [MarshalAs(UnmanagedType.IDispatch)] out object dispatch);

        /// <summary>
        /// Retrieves a scripting-engine-defined identifier for the currently executing thread.
        /// The identifier can be used in subsequent calls to script thread execution-control
        /// methods such as the IActiveScript.InterruptScriptThread method.
        /// </summary>
        /// <param name="threadId">The script thread identifier associated with the current thread.
        /// The interpretation of this identifier is left to the scripting engine, but it can be
        /// just a copy of the Windows thread identifier. If the Win32 thread terminates, this
        /// identifier becomes unassigned and can subsequently be assigned to another thread.</param>
        void GetCurrentScriptThreadID(out uint threadId);

        /// <summary>
        /// Retrieves a scripting-engine-defined identifier for the thread associated with the
        /// given Win32 thread.
        /// </summary>
        /// <param name="win32ThreadId">Thread identifier of a running Win32 thread in the
        /// current process. Use the IActiveScript::GetCurrentScriptThreadID function to
        /// retrieve the thread identifier of the currently executing thread.</param>
        /// <param name="scriptThreadId">The script thread identifier associated with the given
        /// Win32 thread. The interpretation of this identifier is left to the scripting engine,
        /// but it can be just a copy of the Windows thread identifier. Note that if the Win32
        /// thread terminates, this identifier becomes unassigned and may subsequently be
        /// assigned to another thread.</param>
        void GetScriptThreadID(uint win32ThreadId, out uint scriptThreadId);

        /// <summary>
        /// Retrieves the current state of a script thread.
        /// </summary>
        /// <param name="scriptThreadId">Identifier of the thread for which the state is desired.</param>
        /// <param name="threadState"></param>
        void GetScriptThreadState(uint scriptThreadId, out ScriptThreadState threadState);

        /// <summary>
        /// Interrupts the execution of a running script thread (an event sink, an immediate
        /// execution, or a macro invocation). This method can be used to terminate a script that
        /// is stuck (for example, in an infinite loop). It can be called from non-base threads
        /// without resulting in a non-base callout to host objects or to the IActiveScriptSite method.
        /// </summary>
        /// <param name="scriptThreadId">Identifier of the thread to interrupt, or one of the
        /// special thread identifier values.</param>
        /// <param name="exceptionInfo">The error information that should be reported to the aborted script.</param>
        /// <param name="interruptFlags">Option flags associated with the interruption.</param>
        void InterruptScriptThread(uint scriptThreadId, EXCEPINFO exceptionInfo, ScriptInterruptFlags interruptFlags);

        /// <summary>
        /// Clones the current scripting engine (minus any current execution state), returning
        /// a loaded scripting engine that has no site in the current thread. The properties of
        /// this new scripting engine will be identical to the properties the original scripting
        /// engine would be in if it were transitioned back to the initialized state.
        /// </summary>
        /// <param name="script">The cloned scripting engine. The host must create a site and
        /// call the IActiveScript.SetScriptSite method on the new scripting engine before it
        /// will be in the initialized state and, therefore, usable.</param>
        void Clone(out IActiveScript script);
    }
}