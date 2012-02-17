using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.IO;

namespace VS11.Plugin
{
	[Export(typeof(ITestContainerDiscoverer))]
	public class JsTestContainerDiscoverer : ITestContainerDiscoverer
	{
		private IServiceProvider serviceProvider;

		[ImportingConstructor]
		public JsTestContainerDiscoverer([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
		}

		public IEnumerable<ITestContainer> GetTestContainers(ILogger log)
		{
			var solution = (IVsSolution)this.serviceProvider.GetService(typeof(SVsSolution));
			var loadedProjects = VsSolutionHelper.EnumerateLoadedProjects(solution, 
				__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

			return loadedProjects.SelectMany(proj => VsSolutionHelper.GetProjectItems(proj))
                .Where(item => ".js".Equals(Path.GetExtension(item), StringComparison.OrdinalIgnoreCase))
				.Select(item => new JsTestContainer(item, Constants.ExecutorUri));
		}
	}
}
