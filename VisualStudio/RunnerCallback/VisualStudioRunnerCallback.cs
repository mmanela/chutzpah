using System;
using System.Linq;
using Chutzpah.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace Chutzpah.VisualStudio.RunnerCallback
{
    public class VisualStudioRunnerCallback : ITestMethodRunnerCallback
    {
        private readonly DTE2 dte;
        private readonly IVsStatusbar statusBar;
        private OutputWindowPane testPane;

        public VisualStudioRunnerCallback(DTE2 dte, IVsStatusbar statusBar)
        {
            this.dte = dte;
            this.statusBar = statusBar;
        }

        public virtual void TestSuiteStarted()
        {
            dte.ToolWindows.OutputWindow.Parent.Activate();
            dte.ToolWindows.ErrorList.Parent.Activate();
            dte.ToolWindows.OutputWindow.Parent.SetFocus();
            testPane = GetOutputPane("Test");
            testPane.Activate();
            testPane.Clear();
            SetStatusBarMessage("Testing Started");
        }

        public virtual void TestSuiteFinished(TestCaseSummary testResultsSummary)
        {
            var statusBarText = string.Format("{0} passed, {1} failed, {2} total", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            var text = string.Format("========== Total Tests: {0} passed, {1} failed, {2} total ==========\n", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            testPane.OutputString(text);
            SetStatusBarMessage(statusBarText);
        }

        public void FileStarted(string fileName)
        {
            var text = string.Format("------ Test started: File: {0} ------\n", fileName);
            testPane.OutputString(text);
        }

        public virtual void FileFinished(string fileName, TestCaseSummary testResultsSummary)
        {
            var text = string.Format("{0} passed, {1} failed, {2} total (chutzpah).\n\n", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            testPane.OutputString(text);
        }

        public void TestStarted(TestCase testCase)
        {
            throw new NotImplementedException();
        }

        public void TestFinished(TestCase result)
        {
            if (result.Passed)
                TestPassed(result);
            if (!result.Passed)
                TestFailed(result);

            TestComplete(result);
        }

        protected virtual void TestComplete(TestCase result)
        {

        }

        protected virtual void TestFailed(TestCase result)
        {
            var errorMessage = GetTestFailureMessage(result);
            WriteToOutputPaneAndErrorTaskList(result.InputTestFile, errorMessage, errorMessage, result.Line);
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        protected virtual void TestPassed(TestCase result)
        {
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        public void FileError(TestError error)
        {
            var stack = "";
            foreach (var item in error.Stack)
            {
                if (!string.IsNullOrEmpty(item.Function))
                {
                    stack += "at " + item.Function + " ";
                }
                if (!string.IsNullOrEmpty(item.File))
                {
                    stack += "in " + item.File;
                }
                if (!string.IsNullOrEmpty(item.Line))
                {
                    stack += ":line " + item.Line;
                }
            }

            testPane.OutputString(string.Format("Test File Error:\n{0}\n {1}\nWhile Running:{2}\n\n", error.Message, stack,error.InputTestFile));
        }

        public void FileLog(TestLog log)
        {
            testPane.OutputString(string.Format("Log Message: {0} from {1}\n", log.Message, log.InputTestFile));
        }

        public virtual void ExceptionThrown(Exception exception, string fileName)
        {
            testPane.OutputString(string.Format("Chutzpah Error:\n{0}\n While Running:{1}\n\n", exception, fileName));
        }

        protected string GetTestDisplayText(TestCase result)
        {
            return string.IsNullOrWhiteSpace(result.ModuleName)
                       ? result.TestName
                       : string.Format("{0}+{1}", result.ModuleName, result.TestName);
        }

        protected string GetStatusBarMessage(TestCase result)
        {
            var title = GetTestDisplayText(result);
            return string.Format("{0} ({1})", title, result.Passed ? "passed" : "failed");
        }

        protected string GetTestFailureMessage(TestCase testCase)
        {

            var errorString = "";
            foreach (var result in testCase.TestResults.Where(x => !x.Passed))
            {
                if (result.Expected != null || result.Actual != null)
                {
                    errorString += string.Format("Expected: {0}, Actual: {1}\n", result.Expected, result.Actual);
                }
                else if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    errorString += string.Format("{0}", result.Message);
                }
            }
            errorString += string.Format("\n\tin {0}({1},{2}) at {3}\n\n", testCase.InputTestFile, testCase.Line, testCase.Column, GetTestDisplayText(testCase));

            return errorString;
        }


        private OutputWindowPane GetOutputPane(string title)
        {
            OutputWindowPanes panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;

            try
            {
                // If the pane exists already, return it.
                return panes.Item(title);
            }
            catch (ArgumentException)
            {
                // Create a new pane.
                return panes.Add(title);
            }
        }

        private void WriteToOutputPaneAndErrorTaskList(string filePath, string outputPaneText, string taskItemText, int line)
        {
            testPane.OutputTaskItemString(
                outputPaneText, // Output window content
                vsTaskPriority.vsTaskPriorityHigh,
                null,
                vsTaskIcon.vsTaskIconSquiggle,
                filePath,
                line,
                taskItemText, // Task content
                true);
        }

        private void SetStatusBarMessage(string text)
        {
            statusBar.FreezeOutput(0);
            statusBar.SetText(text);
        }
    }
}