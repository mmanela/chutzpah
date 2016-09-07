using Chutzpah.Coverage;
using Chutzpah.Exceptions;
using Chutzpah.FileProcessors;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Server;
using Chutzpah.Utility;
using StructureMap;
using System;
using System.IO;
using System.Reflection;

namespace Chutzpah
{
    public class ChutzpahContainer
    {
        static ChutzpahContainer()
        {
            // Dynamically choose right folder for native dlls
            var success = TryLoadLibuv(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                          || TryLoadLibuv(Environment.CurrentDirectory);

            if (!success)
            {
                throw new ChutzpahException("Unable to load libuv.dll");
            }
        }

        private static bool TryLoadLibuv(string folder)
        {
            var path = Path.Combine(folder, Environment.Is64BitProcess ? "x64" : "x86", "libuv.dll");
            bool ok = File.Exists(path) &&  NativeImports.LoadLibrary(path) != IntPtr.Zero;
            return ok;
        }

        public static IContainer Current
        {
            get { return container; }
        }

        private static readonly IContainer container = CreateContainer();

        private static IContainer CreateContainer()
        {
            var container = new Container();
            container.Configure(config =>
                {
                    config.For<IHasher>().Singleton().Use<Hasher>();
                    config.For<ICoverageEngineFactory>().Singleton().Use<CoverageEngineFactory>();
                    config.For<IChutzpahWebServerFactory>().Singleton().Use<ChutzpahWebServerFactory>();
                    config.For<ICoverageEngine>().Use<BlanketJsCoverageEngine>();
                    config.Scan(scan =>
                        {
                            scan.TheCallingAssembly();
                            scan.WithDefaultConventions();
                            scan.AddAllTypesOf<IQUnitReferencedFileProcessor>();
                            scan.AddAllTypesOf<IJasmineReferencedFileProcessor>();
                            scan.AddAllTypesOf<IMochaReferencedFileProcessor>();
                            scan.AddAllTypesOf<ILineCoverageMapper>();
                            scan.AddAllTypesOf<IFrameworkDefinition>();
                        });
                });

            return container;
        }

        public static T Get<T>()
        {
            return Current.GetInstance<T>();
        }
    }
}