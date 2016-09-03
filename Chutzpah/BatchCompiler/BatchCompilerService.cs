using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Wrappers;
using Chutzpah.FileProcessors;

namespace Chutzpah.BatchProcessor
{
    public class BatchCompilerService : IBatchCompilerService
    {
        private readonly IProcessHelper processHelper;
        private readonly IFileSystemWrapper fileSystem;
        private readonly ISourceMapDiscoverer sourceMapDiscoverer;

        public BatchCompilerService(IProcessHelper processHelper, IFileSystemWrapper fileSystem, ISourceMapDiscoverer sourceMapDiscoverer)
        {
            this.processHelper = processHelper;
            this.fileSystem = fileSystem;
            this.sourceMapDiscoverer = sourceMapDiscoverer;
        }

        public void Compile(IEnumerable<TestContext> testContexts, ITestMethodRunnerCallback callback = null)
        {
            // Group the test contexts by test settings to run batch aware settings like compile
            // For each test settings file that defines a compile step we will run it and update 
            // testContexts reference files accordingly. 
            var groupedTestContexts = testContexts.GroupBy(x => x.TestFileSettings);
            foreach (var contextGroup in groupedTestContexts)
            {
                var testSettings = contextGroup.Key;

                // If there is no compile setting then nothing to do here
                if (testSettings.Compile == null) continue;

                // Build the mapping from source to output files and gather properties about them
                var filePropeties = (
                    from file in contextGroup.SelectMany(x => x.ReferencedFiles).Where(x => !x.IsBuiltInDependency).Distinct()
                    where testSettings.Compile.Extensions.Any(x => file.Path.EndsWith(x, StringComparison.OrdinalIgnoreCase))
                    let sourceProperties = GetFileProperties(file.Path)
                    let sourceHasOutput = !testSettings.Compile.ExtensionsWithNoOutput.Any(x => file.Path.EndsWith(x, StringComparison.OrdinalIgnoreCase))
                    let outputPath = GetOutputPath(file.Path, testSettings.Compile)
                    let outputProperties = sourceHasOutput ? GetFileProperties(outputPath) : null
                    select new SourceCompileInfo { SourceProperties = sourceProperties, OutputProperties = outputProperties, SourceHasOutput = sourceHasOutput }).ToList();

                var outputPathMap = filePropeties
                    .Where(x => x.SourceHasOutput)
                    .ToDictionary(x => x.SourceProperties.Path, x => x.OutputProperties.Path, StringComparer.OrdinalIgnoreCase);

                // Check if the batch compile is needed
                var shouldCompile = CheckIfCompileIsNeeded(testSettings, filePropeties);

                // Run the batch compile if necessary
                if (shouldCompile)
                {
                    if (testSettings.Compile.Mode == BatchCompileMode.Executable)
                    {
                        RunBatchCompile(testSettings);
                    }
                    else 
                    {
                        ChutzpahTracer.TraceWarning("Chutzpah determined generated .js files are missing but the compile mode is External so Chutzpah can't compile them. Test results may be wrong.");
                    }
                }
                else
                {
                    ChutzpahTracer.TraceInformation("Skipping batch compile since all files are update to date for {0}", testSettings.SettingsFileName);
                }

                // Now that compile finished set generated path on  all files who match the compiled extensions
                var filesToUpdate = contextGroup.SelectMany(x => x.ReferencedFiles)
                    .Where(x => outputPathMap.ContainsKey(x.Path));

                foreach (var file in filesToUpdate)
                {
                    var outputPath = outputPathMap[file.Path];
                    if (outputPath != null && fileSystem.FileExists(outputPath))
                    {
                        file.GeneratedFilePath = outputPath;
                        ChutzpahTracer.TraceInformation("Found generated path for {0} at {1}", file.Path, outputPath);
                    }
                    else
                    {
                        // If we could not find the file at the configured path attempt to see if it co-located
                        ChutzpahTracer.TraceInformation("Unable to find generated path at configured location so attempting to see if generated file is co-located.");
                        var coLocatedOutputPath = Path.ChangeExtension(file.Path, ".js");
                        if (fileSystem.FileExists(coLocatedOutputPath))
                        {
                            file.GeneratedFilePath = coLocatedOutputPath;
                            ChutzpahTracer.TraceInformation("Found generated path for {0} at {1}", file.Path, coLocatedOutputPath);
                        }
                        else
                        {

                            var error = string.Format("Couldn't find generated path for {0} at {1} or at {2}", file.Path, outputPath, coLocatedOutputPath);
                            ChutzpahTracer.TraceError(error);

                            if (!testSettings.Compile.IgnoreMissingFiles.GetValueOrDefault())
                            {
                                // Throw and fail here since if we cant find the file we cannot be sure anything will run
                                var exception = new FileNotFoundException(error, outputPath);
                                callback.ExceptionThrown(exception, outputPath);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(file.GeneratedFilePath))
                    {
                        file.SourceMapFilePath = testSettings.Compile.UseSourceMaps.GetValueOrDefault() ? sourceMapDiscoverer.FindSourceMap(file.GeneratedFilePath) : null;
                    }
                }
            }
        }

        private void RunBatchCompile(ChutzpahTestSettingsFile testSettings)
        {
            try
            {
                var result = processHelper.RunBatchCompileProcess(testSettings.Compile);
                if (result.ExitCode > 0)
                {
                    throw new ChutzpahCompilationFailedException(result.StandardOutput + Environment.NewLine + result.StandardError, testSettings.SettingsFileName);
                }
            }
            catch (Exception e)
            {
                ChutzpahTracer.TraceError(e, "Error during batch compile of {0}", testSettings.SettingsFileName);
                throw new ChutzpahCompilationFailedException(e.Message, testSettings.SettingsFileName, e);
            }
        }
        /// <summary>
        /// Determines if a compile is needed. To figure this out we check the following things:
        /// 1. Check if any source file which produces output is missing its output
        /// 2. Check if any source file which produces output is newer than its output
        /// 3. Check if any source file which does not produce output is newer than the oldest output file
        /// </summary>
        /// <param name="testSettings"></param>
        /// <param name="filePropeties"></param>
        /// <returns></returns>
        private static bool CheckIfCompileIsNeeded(ChutzpahTestSettingsFile testSettings, List<SourceCompileInfo> filePropeties)
        {
            if (!filePropeties.Any(x => x.SourceHasOutput))
            {
                return false;
            }

            // If SkipIfUnchanged is true then we check if all the output files are newer than the input files
            // we will only run the compile if this fails
            if (testSettings.Compile.SkipIfUnchanged.GetValueOrDefault())
            {
                var hasMissingOutput = filePropeties
                    .Where(x => x.SourceHasOutput)
                    .Any(x => !x.OutputProperties.Exists);

                if (!hasMissingOutput)
                {


                    var pairFileHasChanged = 
                        filePropeties.Any(x => x.SourceHasOutput
                                               && x.SourceProperties.Exists
                                               && x.OutputProperties.Exists
                                               && x.SourceProperties.LastModifiedDate > x.OutputProperties.LastModifiedDate);


                    var fileWithNoOutputHasChanged = false;
                    var sourcesWithNoOutput = filePropeties.Where(x => x.SourceProperties.Exists && !x.SourceHasOutput).ToList();
                    if (sourcesWithNoOutput.Any())
                    {
                        // Get the time of the newest file change of a file which has no output (like a .d.ts)
                        var newestSourceWithNoOutputFileTime = sourcesWithNoOutput.Max(x => x.SourceProperties.LastModifiedDate);


                        var oldestOutputFileTime = filePropeties
                                                        .Where(x => x.SourceHasOutput && x.OutputProperties.Exists)
                                                        .Min(x => x.OutputProperties.LastModifiedDate);

                        fileWithNoOutputHasChanged = newestSourceWithNoOutputFileTime >= oldestOutputFileTime;
                    }

                    return pairFileHasChanged || fileWithNoOutputHasChanged;
                }
            }

            return true;
        }

        private string GetOutputPath(string filePath, BatchCompileConfiguration compileConfiguration)
        {
            foreach (var pathMap in compileConfiguration.Paths)
            {
                if (filePath.IndexOf(pathMap.SourcePath, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // If the configured sourcePath is a full file path we just assume the fileName is the relative name
                    // Otherwise we calculate the relative path from the configured sourcePath to the current file
                    var relativePath = pathMap.SourcePathIsFile ? Path.GetFileName(pathMap.SourcePath) : UrlBuilder.GetRelativePath(pathMap.SourcePath, filePath);

                    string outputPath = pathMap.OutputPath;
                    if (!pathMap.OutputPathIsFile)
                    {
                        // If output path is not a file we calculate the file path using the input filePath's relative location compared
                        // to the output directory
                        outputPath = Path.Combine(outputPath, relativePath);
                        outputPath = Path.ChangeExtension(outputPath, ".js");

                    }

                    return outputPath;
                }
            }

            ChutzpahTracer.TraceError("Can't find location for generated path on {0}",filePath);

            return null;
        }

        private FileProperties GetFileProperties(string path)
        {
            var fileProperties = new FileProperties();

            if (string.IsNullOrEmpty(path))
            {
                return fileProperties;
            }

            fileProperties.Path = path;
            fileProperties.Exists = fileSystem.FileExists(path);
            fileProperties.LastModifiedDate = fileSystem.GetLastWriteTime(path);

            return fileProperties;
        }


        private class FileProperties
        {
            public DateTime LastModifiedDate { get; set; }
            public string Path { get; set; }
            public bool Exists { get; set; }
        }

        private class SourceCompileInfo
        {
            public SourceCompileInfo()
            {
                SourceHasOutput = true;
            }

            public bool SourceHasOutput { get; set; }
            public FileProperties SourceProperties { get; set; }
            public FileProperties OutputProperties { get; set; }
        }
    }
}