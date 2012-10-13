using System;
using System.Collections.Concurrent;
using System.Threading;
using SassAndCoffee.Core;
using SassAndCoffee.JavaScript;

namespace Chutzpah.Wrappers
{
    /// <summary>
    /// The code below is adapted from the SassAndCoffee project (https://github.com/xpaulbettsx/SassAndCoffee/blob/master/SassAndCoffee.Core/JavascriptInterop.cs)
    /// This gets around an issue with calling the IE engine (which is loaded via COM Interop) from multiple threads. Once you call the engine from a specific thread
    /// then you must always call it from the same thread. 
    /// </summary>
    internal class SingleThreadedJavaScriptHostedCompiler : IDisposable
    {
        class JSWorkItem
        {
            readonly internal ManualResetEventSlim Gate = new ManualResetEventSlim();

            public string Source { get; set; }
            public string Result { get; set; }
            public Type CompilerType { get; set; }

            public JSWorkItem(string source, Type compilerType)
            {
                Source = source;
                CompilerType = compilerType;
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
        static ConcurrentDictionary<Type, JavaScriptCompilerBase> compilers = new ConcurrentDictionary<Type, JavaScriptCompilerBase>();

        static SingleThreadedJavaScriptHostedCompiler()
        {
            DispatcherThread = new Thread(() =>
                                              {
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
                                                          JavaScriptCompilerBase compiler;
                                                          if (!compilers.TryGetValue(item.CompilerType, out compiler))
                                                          {
                                                              compiler = CreateEngine(item.CompilerType);
                                                              compilers[item.CompilerType] = compiler;
                                                          }

                                                          item.Result = compiler.Compile(item.Source);
                                                      }
                                                      catch (Exception ex)
                                                      {
                                                          // Note: You absolutely cannot let any exceptions bubble up, as it kills the app domain.
                                                          item.Result = String.Format("Conversion Error!!! - please report this if it happens frequently: {0}: {1}\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
                                                      }

                                                      item.Gate.Set();
                                                  }
                                              });
            DispatcherThread.IsBackground = true;
            DispatcherThread.Start();
        }

        internal static void shutdownJSThread()
        {
            shouldQuit = true;
            DispatcherThread.Join(TimeSpan.FromSeconds(10));
        }

        private static JavaScriptCompilerBase CreateEngine(Type compilerType)
        {
            var provider = new InstanceProvider<IJavaScriptRuntime>(() => new IEJavaScriptRuntime());
            return (JavaScriptCompilerBase)Activator.CreateInstance(compilerType, provider);
        }

        public string Compile(string sourceCode, Type compilerType)
        {
            var ret = new JSWorkItem(sourceCode, compilerType);
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