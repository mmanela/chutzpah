using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Chutzpah.Compilers.ActiveScript;

namespace Chutzpah.Compilers.JavaScriptEngines
{
    public class IEJavaScriptRuntime : BaseActiveScriptSite, IJavaScriptRuntime, IDisposable
    {
        private IActiveScript jsEngine;
        private IActiveScriptParseWrapper jsParse;
        private object jsDispatch;
        private Type jsDispatchType;

        private readonly Dictionary<string, object> siteItems = new Dictionary<string, object>();

        private const string JavaScriptProgId = "JScript";

        public static bool IsSupported
        {
            get { return Type.GetTypeFromProgID(JavaScriptProgId) != null; }
        }

        public void Initialize()
        {
            try
            {
                // Prefer Chakra
                jsEngine = new ChakraJavaScriptEngine() as IActiveScript;
            }
            catch
            {
                jsEngine = null;
            }

            if (jsEngine == null)
            {
                // No need to catch here - engine of last resort
                jsEngine = new JavaScriptEngine() as IActiveScript;
            }

            jsEngine.SetScriptSite(this);
            jsParse = new ActiveScriptParseWrapper(jsEngine);
            jsParse.InitNew();
        }

        public void LoadLibrary(string libraryCode)
        {
            try
            {
                jsParse.ParseScriptText(libraryCode, null, null, null, IntPtr.Zero, 0, ScriptTextFlags.IsVisible);
            }
            catch
            {
                var last = GetAndResetLastException();
                if (last != null)
                    throw last;
                else throw;
            }
            // Check for parse error
            var parseError = GetAndResetLastException();
            if (parseError != null)
                throw parseError;

            UpdateDispatch();
        }

        public T ExecuteFunction<T>(string functionName, params object[] args)
        {
            T result;
            try
            {
                result =
                    (T) jsDispatchType.InvokeMember(functionName, BindingFlags.InvokeMethod, null, jsDispatch, args);
            }
            catch
            {
                ThrowError();
                throw;
            }

            ThrowError();

            return result;
        }

        public dynamic AsDynamic()
        {
            return jsDispatch;
        }

        private void UpdateDispatch()
        {
            ComRelease(ref jsDispatch);
            jsEngine.GetScriptDispatch(null, out jsDispatch);
            jsDispatchType = jsDispatch.GetType();
        }

        private void ThrowError()
        {
            var last = GetAndResetLastException();
            if (last != null)
                throw last;
        }

        public override object GetItem(string name)
        {
            lock (siteItems)
            {
                object result = null;
                return siteItems.TryGetValue(name, out result) ? result : null;
            }
        }

        public override IntPtr GetTypeInfo(string name)
        {
            lock (siteItems)
            {
                if (!siteItems.ContainsKey(name))
                    return IntPtr.Zero;
                return Marshal.GetITypeInfoForType(siteItems[name].GetType());
            }
        }

        ~IEJavaScriptRuntime()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            ComRelease(ref jsDispatch, !disposing);

            // For now these next two actually reference the same object, but it doesn't hurt to be explicit.
            ComRelease(ref jsParse, !disposing);
            ComRelease(ref jsEngine, !disposing);
        }

        private void ComRelease<T>(ref T o, bool final = false)
            where T : class
        {
            if (o != null && Marshal.IsComObject(o))
            {
                if (final)
                    Marshal.FinalReleaseComObject(o);
                else
                    Marshal.ReleaseComObject(o);
            }
            o = null;
        }
    }
}