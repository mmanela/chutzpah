using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chutzpah;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace VS11.Plugin
{
	public class JsTestContainer : ITestContainer
	{
        private readonly DateTime timeStamp;
        private ITestContainerDiscoverer discoverer;

        public JsTestContainer(ITestContainerDiscoverer discoverer, string source, Uri executorUri)
            : this(discoverer, source, executorUri, Enumerable.Empty<Guid>())
		{
		}

        public JsTestContainer(ITestContainerDiscoverer discoverer, string source, Uri executorUri, IEnumerable<Guid> debugEngines)
		{
            ValidateArg.NotNullOrEmpty(source, "source");
            ValidateArg.NotNull(executorUri, "executorUri");
            ValidateArg.NotNull(debugEngines, "debugEngines");
            ValidateArg.NotNull(discoverer, "discoverer");

			this.Source = source;
            this.ExecutorUri = executorUri;
			this.TargetFramework = FrameworkVersion.None;
            this.TargetPlatform = Architecture.AnyCPU;
			this.DebugEngines = debugEngines;
            this.timeStamp = GetTimeStamp();
            this.discoverer = discoverer;
		}

        
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy"></param>
        private JsTestContainer(JsTestContainer copy)
            : this(copy.Discoverer, copy.Source, copy.ExecutorUri)
        {
            this.timeStamp = copy.timeStamp;
        }

        /// <summary>
        /// Returns the debug engine(s) to be used by VisualStudio debugger
        /// </summary>
		public IEnumerable<Guid> DebugEngines { get; private set; }

        /// <summary>
        /// The test adapter Uri for this container
        /// </summary>
		public Uri ExecutorUri { get; private set; }


        /// <summary>
        /// Return test container source which allows the test adapter to discover tests within it.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Target .net framework of this test container. Use FrameworkVersion.None if it this property is not applicable. 
        /// </summary>
		public FrameworkVersion TargetFramework { get; private set; }

        /// <summary>
        /// Target platform of this test container. 
        /// </summary>
		public Architecture TargetPlatform { get; private set; }

		public override string ToString()
		{
			return this.ExecutorUri.ToString() + "/" + this.Source;
		}

        /// <summary>
        /// Deploys the Metro test container and returns info about the deployment, or does nothing and returns null if this is not a Metro test container.
        /// </summary>
		public IDeploymentData DeployAppContainer() { return null; }

        /// <summary>
        ///True if this is a test container for Win8 Metro tests. False otherwise.
        /// </summary>
		public bool IsAppContainerTestContainer { get { return false; } }


        /// <summary>
        /// Comapre this test container to another one
        /// They are the same if same source and timestamp
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ITestContainer other)
        {
            var testContainer = other as JsTestContainer;
            if (testContainer == null)
            {
                return -1;
            }

            var result = String.Compare(this.Source, testContainer.Source, StringComparison.OrdinalIgnoreCase);
            if (result != 0)
            {
                return result;
            }

            ChutzpahTracer.TraceInformation("Test container comparision {0} vs {1} for {2}", this.timeStamp, testContainer.timeStamp, this.Source);

            return this.timeStamp.CompareTo(testContainer.timeStamp);
        }

        public ITestContainerDiscoverer Discoverer
        {
            get { return discoverer; }
        }

        /// <summary>
        /// Time stamp of when this testcontainer was last written.
        /// </summary>
        private DateTime GetTimeStamp()
        {
            if (!String.IsNullOrEmpty(this.Source) && File.Exists(this.Source))
            {
                return File.GetLastWriteTime(this.Source);
            }
            else
            {
                return DateTime.MinValue;
            }
        }


        /// <summary>
        /// Creates an snapshot in time of the container
        /// </summary>
        /// <returns></returns>
        public ITestContainer Snapshot()
        {
            return new JsTestContainer(this);
        }
    }
}
