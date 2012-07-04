using Chutzpah.FileProcessors;
using Chutzpah.FrameworkDefinitions;
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
                    config.Scan(scan =>
                        {
                            scan.TheCallingAssembly();
                            scan.WithDefaultConventions();
                            scan.AddAllTypesOf<IQUnitReferencedFileProcessor>();
                            scan.AddAllTypesOf<IJasmineReferencedFileProcessor>();
                            scan.AddAllTypesOf<IFrameworkDefinition>();
                        });
                });

            return container;
        }
    }
}