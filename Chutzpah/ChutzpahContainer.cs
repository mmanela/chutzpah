namespace Chutzpah
{
    using Chutzpah.FileProcessors;
    using Chutzpah.FrameworkDefinitions;
    using StructureMap;

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
                    scan.AddAllTypesOf<IQUnitReferencedFileProcessor>();
                    scan.AddAllTypesOf<IJasmineReferencedFileProcessor>();
                    scan.AddAllTypesOf<IFrameworkDefinition>();
                });
            });

            return container;
        }
    }
}
