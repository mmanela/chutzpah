using System;
using System.Collections.Concurrent;
using System.Threading;
using SassAndCoffee.Core;
using SassAndCoffee.JavaScript;
using SassAndCoffee.JavaScript.CoffeeScript;

namespace Chutzpah.Wrappers
{
    public interface ICoffeeScriptEngineWrapper
    {
        string Compile(string coffeScriptSource);
    }

    public class CoffeeScriptEngineWrapper : ICoffeeScriptEngineWrapper
    {
        private readonly SingleThreadedCoffeeScriptCompiler engine;

        public CoffeeScriptEngineWrapper()
        {
            engine = new SingleThreadedCoffeeScriptCompiler();
        }

        public string Compile(string coffeScriptSource)
        {
            return engine.Compile(coffeScriptSource);
        }
    }


    /// <summary>
    /// The code below is adapted from the SassAndCoffee project (https://github.com/xpaulbettsx/SassAndCoffee/blob/master/SassAndCoffee.Core/JavascriptInterop.cs)
    /// This gets around an issue with calling the IE engine (which is loaded via COM Interop) from multiple threads. Once you call the engine from a specific thread
    /// then you must always call it from the same thread. 
    /// </summary>
    class SingleThreadedCoffeeScriptCompiler : IDisposable
    {
        class JSWorkItem
        {
            readonly internal ManualResetEventSlim Gate = new ManualResetEventSlim();

            public string Source { get; set; }
            public string Result { get; set; }

            public JSWorkItem(string source)
            {
                Source = source;
            }

            public string GetValueSync()
            {
                Gate.Wait();
                return Result;
            }
        }

        static ConcurrentQueue<JSWorkItem> workQueue = new ConcurrentQueue<JSWorkItem>();
        static readonly Thread DispatcherThread;
        static bool shouldQuit;

        static SingleThreadedCoffeeScriptCompiler()
        {
            DispatcherThread = new Thread(() =>
            {
                var engine = new Lazy<CoffeeScriptCompiler>(CreateEngine);

                while (!shouldQuit)
                {
                    if (workQueue == null)
                    {
                        break;
                    }

                    JSWorkItem item;
                    if (!workQueue.TryDequeue(out item))
                    {
                        Thread.Sleep(100);

                        continue;
                    }

                    try
                    {
                        item.Result = engine.Value.Compile(item.Source);
                    }
                    catch (Exception ex)
                    {
                        // Note: You absolutely cannot let any exceptions bubble up, as it kills the app domain.
                        item.Result = String.Format("CoffeeScript Conversion Error!!! - please report this if it happens frequently: {0}: {1}\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
                    }

                    item.Gate.Set();
                }
            });
            DispatcherThread.IsBackground = true;
            DispatcherThread.Start();
        }

        private static CoffeeScriptCompiler CreateEngine()
        {
            var provider = new InstanceProvider<IJavaScriptRuntime>(() => new IEJavaScriptRuntime());
            var compiler = new CoffeeScriptCompiler(provider);
            return compiler;
        }

        internal static void shutdownJSThread()
        {
            shouldQuit = true;
            DispatcherThread.Join(TimeSpan.FromSeconds(10));
        }

        public string Compile(string coffeeScriptCode)
        {
            var ret = new JSWorkItem(coffeeScriptCode);
            workQueue.Enqueue(ret);
            return ret.GetValueSync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            workQueue = null;
        }
    }
}