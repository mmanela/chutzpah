using System;
using System.IO;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah.RunnerCallbacks
{
    public class ConsoleRunnerCallback : RunnerCallback
    {
        public override void ExceptionThrown(Exception exception, string fileName)
        {
            Console.Write(GetExceptionThrownMessage(exception, fileName));
        }

        public override void FileError(TestError error)
        {
            var errorMessage = GetFileErrorMessage(error);
            Console.Write(errorMessage);
        }

        public override void FileFinished(string fileName, TestFileSummary testResultsSummary)
        {
            if (testResultsSummary.CoverageObject != null)
            {
                var folder = Path.GetDirectoryName(fileName);
                var coverageFileName = Path.GetFileNameWithoutExtension(fileName) + ".coverage.json";
                File.WriteAllText(Path.Combine(folder, coverageFileName), testResultsSummary.CoverageObject.ToString());
            }

            base.FileFinished(fileName, testResultsSummary);
        }
    }
}