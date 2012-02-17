using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace VS11.Plugin
{
	public class JsTestContainer : ITestContainer
	{
		public JsTestContainer(string source, Uri uri)
			: this(source, uri, Enumerable.Empty<Guid>())
		{
		}

		public JsTestContainer(string source, Uri uri, IEnumerable<Guid> debugEngines)
		{
			this.Source = source;
			this.ExecutorUri = uri;
			this.TargetFramework = FrameworkVersion.Framework45;
			this.DebugEngines = debugEngines;
		}

		public IEnumerable<Guid> DebugEngines { get; private set; }
		public Uri ExecutorUri { get; private set; }
		public string Source { get; private set; }
		public FrameworkVersion TargetFramework { get; private set; }
		public Architecture TargetPlatform { get; private set; }

		public override string ToString()
		{
			return this.ExecutorUri.ToString() + "/" + this.Source;
		}

		#region Unused AppContainer stuff
		public IDeploymentData DeployAppContainer() { return null; }
		public bool IsAppContainerTestContainer { get { return false; } }
		#endregion
	}
}
