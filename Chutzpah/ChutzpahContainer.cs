using Chutzpah.Coverage;
using Chutzpah.FileProcessors;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Server;
using Chutzpah.Utility;
using StructureMap;

namespace Chutzpah
{
    public class ChutzpahContainer
    {
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