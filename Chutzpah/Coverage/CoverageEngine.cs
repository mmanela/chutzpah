using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Chutzpah.Exceptions;
using Chutzpah.Models;
using Chutzpah.Utility;
using Chutzpah.Wrappers;
using System.Text.RegularExpressions;

namespace Chutzpah.Coverage
{
    public class CoverageEngine : ICoverageEngine
    {
        private const string JSCover_JAR = "JSCover-all.jar";
        private const string JSCover_URL = "http://tntim96.github.com/JSCover/";
        private const string JSCover_CmdLineFmt = "-jar \"{0}\" -fs --branch \"{1}\" \"{2}\"";
        private const string JSCover_HelperJS = "jscoverage.js";

        private readonly IProcessHelper processHelper;
        private readonly IFileSystemWrapper fileSystem;
        private readonly IHasher hasher;

        private readonly string jsCoverJarPath;
        private readonly string javaExePath;

        public CoverageEngine(IFileSystemWrapper fileSystem, IProcessHelper processHelper, IFileProbe fileProbe, IHasher hasher)
        {
            this.fileSystem = fileSystem;
            this.processHelper = processHelper;
            this.hasher = hasher;

            jsCoverJarPath = fileProbe.FindFilePath(JSCover_JAR);
            javaExePath = FindJavaExe();
        }

        public string IncludePattern { get; set; }
        
        public string ExcludePattern { get; set; }

        public void Instrument(string stagingFolder, IList<ReferencedFile> referencedFiles, IList<string> temporaryFiles, out string coverageHelperPath)
        {
            var aName = Path.GetFileNameWithoutExtension(referencedFiles.First().Path);
            var theOnesToInstrument = referencedFiles.Where(f =>
            {
                var originalPath = f.Path;
                var path = f.GeneratedFilePath ?? originalPath;
                return ShouldInstrument(originalPath, path);
            }).ToList();
            Instrument(stagingFolder, aName, theOnesToInstrument, temporaryFiles, out coverageHelperPath);
        }

        public bool CanUse(IList<string> messages)
        {
            var hasNullPath = javaExePath == null || jsCoverJarPath == null;
            if (hasNullPath && messages != null)
            {
                messages.Add("Coverage collection using JSCover will be disabled based on the following reason(s):");
                if (javaExePath == null)
                {
                    messages.Add("- Failed to find java.exe. Make sure that it's on the PATH, or that JAVA_HOME is set.");
                }
                if (jsCoverJarPath == null)
                {
                    messages.Add(
                        string.Format(
                            "- Failed to find {0}. Download it from {1} and place it in the current directory or where Chutzpah is located.",
                            JSCover_JAR, JSCover_URL));
                }
            }

            return !hasNullPath;
        }

        private void Instrument(string stagingFolder, string aName, IList<ReferencedFile> referencedFiles, IList<string> temporaryFiles, out string coverageHelperPath)
        {
            coverageHelperPath = null;
            if (referencedFiles.Count == 0) return;

            var inputFolder = fileSystem.GetTemporaryFolder(hasher.Hash("_in_" + aName));
            var outputFolder = fileSystem.GetTemporaryFolder(hasher.Hash("_out_" + aName));
            CreateFolders(inputFolder, outputFolder);
            try
            {
                CopyFilesToInstrumentationSourceDir(referencedFiles, inputFolder);
                var result = DoInstrumentation(inputFolder, outputFolder);
                CheckInstrumentationFailure(result);
                var newTempFiles = CopyFilesFromInstrumentationDest(referencedFiles, outputFolder);
                newTempFiles.ForEach(temporaryFiles.Add);

                // jscoverage.js is needed when running (to get JSON)
                var jscoverageJsSourcePath = Path.Combine(outputFolder, JSCover_HelperJS);
                var jscoverageJsNewName = string.Format(Constants.ChutzpahTemporaryFileFormat, JSCover_HelperJS);
                var jscoverageJsDestPath = Path.Combine(stagingFolder, jscoverageJsNewName);
                fileSystem.CopyFile(jscoverageJsSourcePath, jscoverageJsDestPath);

                temporaryFiles.Add(jscoverageJsDestPath);
                coverageHelperPath = jscoverageJsDestPath;
            }
            finally
            {
                DeleteFolders(inputFolder, outputFolder);
            }
        }

        private List<string> CopyFilesFromInstrumentationDest(IList<ReferencedFile> referencedFiles, string outputFolder)
        {
            var tempFiles = new List<string>();
            // Read back all instrumented files and copy to their proper location
            // (new temporary file if necessary).
            for (var i = 0; i < referencedFiles.Count; i++)
            {
                var tempFileName = "file" + i + ".js";
                var originalPath = referencedFiles[i].Path;
                var instrPath = Path.Combine(outputFolder, tempFileName);

                var folderPath = Path.GetDirectoryName(originalPath);
                var fileName = Path.GetFileNameWithoutExtension(originalPath) + ".js";

                var newFilePath = Path.Combine(folderPath, string.Format(Constants.ChutzpahInstrumentedTemporaryFileFormat, fileName));

                // This is a bit lame, but the alternative would be to create the entire directory
                // tree in the instrumentation source folder, and that would open up a whole lot
                // of other problems. Backslash escaping is necessary since the string will be
                // interpreted by JavaScript.
                var escapedPath = originalPath.Replace("'", "\\'").Replace("\\", "\\\\");
                ReadWrite(instrPath, newFilePath, "(_\\$jscoverage(\\.branchData)?)\\['" + Regex.Escape(tempFileName) + "'\\]",
                          "$1['" + escapedPath + "']");

                referencedFiles[i].GeneratedFilePath = newFilePath;
                tempFiles.Add(newFilePath);
            }
            return tempFiles;
        }

        private void CopyFilesToInstrumentationSourceDir(IList<ReferencedFile> referencedFiles, string inputFolder)
        {
            // Write out all files with dummy names (to avoid having to recreate the
            // directory structure and/or deal with name conflicts).
            for (var i = 0; i < referencedFiles.Count; i++)
            {
                var sourcePath = referencedFiles[i].GeneratedFilePath ?? referencedFiles[i].Path;
                var destPath = Path.Combine(inputFolder, "file" + i + ".js");

                // Do read/write instead of copy to get rid of UTF-8 BOM.
                // (An alternative is to detect UTF-8 BOM and set -Dfile.encoding=utf-8)
                ReadWrite(sourcePath, destPath);
            }
        }

        private void CheckInstrumentationFailure(ProcessResult<IList<string>> result)
        {
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
        }

        private ProcessResult<IList<string>> DoInstrumentation(string inputFolder, string outputFolder)
        {
            var args = string.Format(JSCover_CmdLineFmt, jsCoverJarPath, inputFolder, outputFolder);
            var result = processHelper.RunExecutableAndProcessOutput(javaExePath, args, OutputCollector());
            return result;
        }

        private void ReadWrite(string sourcePath, string destPath, string replaceThisRegexp = null, string withThis = null)
        {
            var text = fileSystem.GetText(sourcePath);
            if (replaceThisRegexp != null)
            {
                var re = new Regex(replaceThisRegexp);
                text = re.Replace(text, withThis);
            }
            fileSystem.WriteAllText(destPath, text);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);

        private string FindJavaExe()
        {
            const string exe = "java.exe";
            try
            {
                // First check if it's on the path
                processHelper.RunExecutableAndProcessOutput(exe, "-version", OutputCollector());
                return exe; // no exception implies success
            }
            catch (Win32Exception)
            {
                // Likely: "The system cannot find the file specified"
            }

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
