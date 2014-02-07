using System.Collections.Generic;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;

namespace Chutzpah.Coverage
{
    public interface ICoverageEngine
    {
        /// <summary>
        /// Gets a list of file dependencies to bundle with the framework test harness.
        /// </summary>
        /// <param name="definition">The test framework used.</param>
        /// <param name="testSettings"></param>
        IEnumerable<string> GetFileDependencies(IFrameworkDefinition definition, ChutzpahTestSettingsFile testSettings);


        /// <summary>
        /// Modifies the test harness for coverage instrumentation and collection. This method is
        /// also expected to inject some script code that sets a variable in JavaScripts top-level
        /// <c>window</c> object named after <see cref="Constants.ChutzpahCoverageObjectReference" />
        /// with a value that is the actual name of the generated coverage object.
        /// </summary>
        /// <param name="harness">The test harness to modify.</param>
        /// <param name="definition">The test framework used.</param>
        /// <param name="testSettings"></param>
        void PrepareTestHarnessForCoverage(TestHarness harness, IFrameworkDefinition definition, ChutzpahTestSettingsFile testSettings);

        /// <summary>
        /// Deserializes the JSON representation of the coverage object and adapts it to Chutzpah's
        /// coverage data format.
        /// </summary>
        /// <param name="json">The JSON representation of the coverage object.</param>
        /// <param name="testContext">The current test context.</param>
        /// <returns>A coverage object.</returns>
        CoverageData DeserializeCoverageObject(string json, TestContext testContext);

        /// <summary>
        /// Add file name pattern that, if set, a file must match to be instrumented. Pattern matching
        /// is done with the <c>PathMatchSpec</c> Windows function.
        /// </summary>
        void AddIncludePatterns(IEnumerable<string> includePatterns);

        
        /// Add file name pattern that, if set, a file must NOT match to be instrumented. Pattern matching 
        /// is done with the <c>PathMatchSpec</c> Windows function.
        void AddExcludePatterns(IEnumerable<string> excludePatterns);
    }
}
