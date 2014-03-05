using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Exceptions;
using Chutzpah.Models;

namespace Chutzpah.BatchProcessor
{
    public class BatchCompilerService
    {
        private readonly IProcessHelper processHelper;

        public BatchCompilerService(IProcessHelper processHelper)
        {
            this.processHelper = processHelper;
        }


        public void Compile(IEnumerable<TestContext> testContexts)
        {
             // Group the test contexts by test settings to run batch aware settings like compile
            // For each test settings file that defines a compile step we will run it and update 
            // testContexts reference files accordingly. 
            var groupedTestContexts = testContexts.GroupBy(x => x.TestFileSettings);
            foreach (var contextGroup in groupedTestContexts)
            {
                var testSettings = contextGroup.Key;
                if (testSettings.Compile == null) continue;

                // If we have a compile setting for this batch then execute the process 
                // and set generated paths that match extension
                
                var result = processHelper.RunBatchCompileProcess(testSettings.Compile);
                if (result.ExitCode > 0)
                {
                    throw new ChutzpahCompilationFailedException(result.StandardError);
                }

                // Now that compile finished set generated path on  all files who match the compiled extensions
                var allFiles = contextGroup.SelectMany(x => x.ReferencedFiles);
                foreach (var file in allFiles)
                {
                    if (testSettings.Compile.Extensions.Any(x => file.Path.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Figure out if this file was compiled and mark its generate path correctly

                        // See if it is in the source dir, if not we must ignore it
                        if (file.Path.IndexOf(testSettings.Compile.SourceDirectory, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            var relativePath = FileProbe
                        }
                        else
                        {
                            ChutzpahTracer.TraceWarning(
                                "Not setting generated path on {0} since it is not inside of configured source dir {1}",
                                file.Path,
                                testSettings.Compile.SourceDirectory);
                        }
                    }
                }
                
            }
            
        }
    }
}
