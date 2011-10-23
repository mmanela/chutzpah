using Chutzpah.FrameworkDefinitions;
using StructureMap;

namespace Chutzpah
{
    public class ChutzpahContainer
    {
        public static IContainer Current
        {
            get { return container; }
        }

        private static IContainer container = CreateContainer();

        private static IContainer CreateContainer()
        {
            var container = new Container();
            container.Configure(config =>
            {
                config.Scan(scan =>
                {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
                    scan.AddAllTypesOf<IReferencedFileProcessor>();
                    scan.AddAllTypesOf<IFrameworkDefinition>();
                });
            });


            return container;
        }
    }
}
