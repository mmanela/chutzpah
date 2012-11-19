using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Utility;

namespace Chutzpah.Wrappers
{
    public class CoverageEngineWrapper : ICoverageEngineWrapper
    {
        private const string JSCover_jar = "JSCover-all.jar";

        private readonly IProcessHelper processHelper;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IHasher hasher;

        private readonly string jsCoverJarPath;
        private readonly string javaExePath;

        public CoverageEngineWrapper(IFileSystemWrapper fileSystem, IProcessHelper processHelper, IFileProbe fileProbe, IHasher hasher)
        {
            this.fileSystem = fileSystem;
            this.processHelper = processHelper;
            this.hasher = hasher;

            jsCoverJarPath = fileProbe.FindFilePath(JSCover_jar);
            javaExePath = FindJavaExe();

            // Wouldn't hurt with some debug logging here.
            if (jsCoverJarPath == null || javaExePath == null)
            {
                throw new ChutzpahException("Failed to find " + JSCover_jar +
                                            " or Java executable, both required for coverage collection.");
            }
        }

        public void Instrument(IList<ReferencedFile> referencedFiles, IList<string> temporaryFiles)
        {
            var aName = Path.GetFileNameWithoutExtension(referencedFiles.First().Path);
            var theOnesToInstrument = referencedFiles.Where(f =>
            {
                var originalPath = f.Path;
                var path = f.GeneratedFilePath ?? originalPath;
                return ShouldInstrument(originalPath, path);
            }).ToList();
            Instrument(aName, theOnesToInstrument, temporaryFiles);
        }

        private void Instrument(string aName, IList<ReferencedFile> referencedFiles, IList<string> temporaryFiles)
        {
            if (referencedFiles.Count == 0) return;

            var inputFolder = fileSystem.GetTemporaryFolder(hasher.Hash("_in_" + aName));
            var outputFolder = fileSystem.GetTemporaryFolder(hasher.Hash("_out_" + aName));
            CreateFolders(inputFolder, outputFolder);
            try
            {
                // Write out all files with dummy names (to avoid having to recreate the
                // directory structure and/or deal with name conflicts).
                for (var i = 0; i < referencedFiles.Count; i++)
                {
                    var sourcePath = referencedFiles[i].GeneratedFilePath ?? referencedFiles[i].Path;
                    var destPath = Path.Combine(inputFolder, "file" + i + ".js");
                    // Do read/write instead of copy to get rid of UTF-8 BOM.
                    fileSystem.WriteAllText(destPath, fileSystem.GetText(sourcePath));
                }

                var result = DoInstrumentation(inputFolder, outputFolder);
                // Currently, JSCover may exit with status 0 even in case of error, so we have to check the output.
                if (result.ExitCode != 0 || result.Model.Count > 0)
                {
                    var msg = result.Model.Count > 0
                                  ? string.Join(Environment.NewLine,
                                                Enumerable.Repeat("Source code instrumentation failed:", 1).Concat(
                                                    result.Model))
                                  : "Instrumentation failed with code " + result.ExitCode;
                    throw new ChutzpahException(msg);
                }

                // Read back all instrumented files and copy to their proper location
                // (new temporary file if necessary).
                for (var i = 0; i < referencedFiles.Count; i++)
                {
                    var originalPath = referencedFiles[i].Path;
                    var instrPath = Path.Combine(outputFolder, "file" + i + ".js");
                    var instrumentedText = FixPath(originalPath, "file" + i + ".js", fileSystem.GetText(instrPath));

                    var folderPath = Path.GetDirectoryName(originalPath);
                    var fileName = Path.GetFileNameWithoutExtension(originalPath) + ".js";

                    var newFilePath = Path.Combine(folderPath, string.Format(Constants.ChutzpahInstrumentedTemporaryFileFormat, fileName));

                    fileSystem.WriteAllText(newFilePath, instrumentedText);
                    referencedFiles[i].GeneratedFilePath = newFilePath;
                    temporaryFiles.Add(newFilePath);
                }
            }
            finally
            {
                DeleteFolders(inputFolder, outputFolder);
            }
        }

        private ProcessResult<IList<string>> DoInstrumentation(string inputFolder, string outputFolder)
        {
            var args = string.Format("-jar \"{0}\" -fs \"{1}\" \"{2}\"", jsCoverJarPath, inputFolder, outputFolder);
            var result = processHelper.RunExecutableAndProcessOutput(javaExePath, args, OutputCollector());
            return result;
        }

        private string FixPath(string realPath, string tempPath, string text)
        {
            // Path replacement is a bit lame, but the alternative would be to create the entire
            // directory tree in the instrumentation source folder, and that would open up a
            // whole lot of other problems. Backslash escaping is necessary since the string will
            // be interpreted by JavaScript.
            var escapedPath = realPath.Replace("'", "\\'").Replace("\\", "\\\\");
            return text.Replace("_$jscoverage['" + tempPath + "']", "_$jscoverage['" + escapedPath + "']");
        }

        public string IncludePattern { get; set; }
        public string ExcludePattern { get; set; }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);

        private string FindJavaExe()
        {
            const string exe = "java.exe";
            // First check if it's on the path
            var result = processHelper.RunExecutableAndProcessOutput(exe, "-version", OutputCollector());
            if (result.ExitCode == 0) return exe;

            // Next, check JAVA_HOME\bin
            var pathToExe = Environment.ExpandEnvironmentVariables(Path.Combine("%JAVA_HOME%", "bin", exe));
            if (fileSystem.FileExists(pathToExe)) return pathToExe;

            return null; // no luck
        }

        private Func<ProcessStream, IList<string>> OutputCollector()
        {
            return stream => EnumerateStream(stream.StreamReader).TakeWhile(s => s != null).ToList();
        }

        private IEnumerable<string> EnumerateStream(StreamReader stream)
        {
            while (true)
            {
                yield return stream.ReadLine();
            }
        }

        private void CreateFolders(params string[] folders)
        {
            foreach (var folder in folders)
            {
                if (fileSystem.FolderExists(folder))
                {
                    fileSystem.DeleteDirectory(folder, true);
                }
                fileSystem.CreateDirectory(folder);
            }
        }

        private void DeleteFolders(params string[] folders)
        {
            foreach (var folder in folders)
            {
                fileSystem.DeleteDirectory(folder, true);
            }
        }

        private bool ShouldInstrument(string originalPath, string path)
        {
            // Assume (for now) that only JS files can be instrumented.
            if (!path.EndsWith(Constants.JavaScriptExtension, StringComparison.OrdinalIgnoreCase)) return false;

            // Include/exclude patterns are written for the original path rather than 
            // a temporary file, if any.
            return IsFileEligibleForInstrumentation(originalPath);
        }

        private bool IsFileEligibleForInstrumentation(string filePath)
        {
            if (IncludePattern != null && !PathMatchSpec(filePath, IncludePattern)) return false;
            if (ExcludePattern != null && PathMatchSpec(filePath, ExcludePattern)) return false;
            return true;
        }
    }
}
