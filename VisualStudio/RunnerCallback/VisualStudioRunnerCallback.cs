using System;
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

        public virtual void TestSuiteFinished(TestResultsSummary testResultsSummary)
        {
            var statusBarText = string.Format("{0} passed, {1} failed, {2} total", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            var text = string.Format("========== Total Tests: {0} passed, {1} failed, {2} total ==========\n", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            testPane.OutputString(text);
            SetStatusBarMessage(statusBarText);
        }

        public virtual void ExceptionThrown(Exception exception, string fileName)
        {
            testPane.OutputString(string.Format("Chutzpah Error Occured:\n{0}\n While Running:{1}\n\n", exception, fileName));
        }

        public virtual bool FileStart(string fileName)
        {
            var text = string.Format("------ Test started: File: {0} ------\n", fileName);
            testPane.OutputString(text);
            return true;
        }

        public virtual bool FileFinished(string fileName, TestResultsSummary testResultsSummary)
        {
            var text = string.Format("{0} passed, {1} failed, {2} total (chutzpah).\n\n", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            testPane.OutputString(text);
            return true;
        }

        public void TestFinished(TestResult result)
        {
            TestStarted(result);
            if (result.Passed)
                TestPassed(result);
            if (!result.Passed)
                TestFailed(result);

            TestComplete(result);
        }

        protected virtual void TestComplete(TestResult result)
        {

        }

        protected virtual void TestFailed(TestResult result)
        {
            var errorMessage = GetTestFailureMessage(result);
            WriteToOutputPaneAndErrorTaskList(result.InputTestFile, errorMessage, errorMessage, result.Line);
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        protected virtual void TestStarted(TestResult result)
        {
        }

        protected virtual void TestPassed(TestResult result)
        {
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        protected string GetTestDisplayText(TestResult result)
        {
            return string.IsNullOrWhiteSpace(result.ModuleName)
                       ? result.TestName
                       : string.Format("{0}+{1}", result.ModuleName, result.TestName);
        }

        protected string GetStatusBarMessage(TestResult result)
        {
            var title = GetTestDisplayText(result);
            return string.Format("{0} ({1})", title, result.Passed ? "passed" : "failed");
        }

        protected string GetTestFailureMessage(TestResult result)
        {
            var title = GetTestDisplayText(result);
            var failureMessage = string.Format("Test '{0}' failed\n", title);
            if (result.Expected != null || result.Actual != null)
            {
                failureMessage += string.Format("Expected: {0}, Actual: {1}", result.Expected, result.Actual);
            }
            else if (!string.IsNullOrWhiteSpace(result.Message))
            {
                failureMessage += string.Format("{0}", result.Message);
            }

            failureMessage += string.Format("   at {0} in {1}\n\n", title, result.InputTestFile);

            return failureMessage;
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
                outputPaneText, // Output window text
                vsTaskPriority.vsTaskPriorityHigh,
                null,
                vsTaskIcon.vsTaskIconSquiggle,
                filePath,
                line,
                taskItemText, // Task text
                true);
        }

        private void SetStatusBarMessage(string text)
        {
            statusBar.FreezeOutput(0);
            statusBar.SetText(text);
        }
    }
}