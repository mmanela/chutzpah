using Nancy;
using Nancy.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Server
{
    public class NancySettingsBootstrapper : Nancy.DefaultNancyBootstrapper
    {
        readonly string rootPath;

        public NancySettingsBootstrapper(string rootPath)
        {
            this.rootPath = rootPath;
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("chutzpah", @"dev/chutzpah/Samples/RequireJS/QUnit"));

            base.ConfigureConventions(nancyConventions);
        }

        protected override IEnumerable<Type> ViewEngines
        {
            get
            {
                return Enumerable.Empty<Type>();
            }
        }

        protected override IRootPathProvider RootPathProvider
        {
            get
            {
                return new NancyRootPathProvider(rootPath);
            }
        }
    }
}
