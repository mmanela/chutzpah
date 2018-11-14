using System;
using System.Linq;
using Chutzpah.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Chutzpah.VisualStudioContextMenu;

namespace Chutzpah.VisualStudio.Callback
{
    public class VisualStudioRunnerCallback : RunnerCallback
    {
        private readonly DTE2 dte;
        private readonly IVsStatusbar statusBar;
        private OutputWindowPane testPane;

        public VisualStudioRunnerCallback(DTE2 dte, IVsStatusbar statusBar)
        {
            this.dte = dte;
            this.statusBar = statusBar;
        }

        public override void TestSuiteStarted(TestContext context)
        {
            dte.ToolWindows.OutputWindow.Parent.Activate();
            dte.ToolWindows.ErrorList.Parent.Activate();
            dte.ToolWindows.OutputWindow.Parent.SetFocus();
            testPane = GetOutputPane("Test");
            testPane.Activate();
            testPane.Clear();
            SetStatusBarMessage("Testing Started");
        }

        public override void TestSuiteFinished(TestContext context, TestCaseSummary testResultsSummary)
        {
            var statusBarText = "";
            if (testResultsSummary.SkippedCount > 0)
            {
                statusBarText = string.Format("{0} passed, {1} failed, {2} skipped, {3} total", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.SkippedCount, testResultsSummary.TotalCount);
            }
            else
            {
                statusBarText = string.Format("{0} passed, {1} failed, {2} total", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            }

            var text = string.Format("========== Total Tests: {0} ==========\n", statusBarText);
            testPane.OutputString(text);
            SetStatusBarMessage(statusBarText);
        }

        public override void FileStarted(TestContext context)
        {
            var text = string.Format("------ Test started: File: {0} ------\n", context?.InputTestFilesString);
            testPane.OutputString(text);
        }

        public override void FileFinished(TestContext context, TestFileSummary testResultsSummary)
        {
            var text = "";

            if (testResultsSummary.SkippedCount <= 0)
            {
                text = string.Format("{0} passed, {1} failed, {2} total (chutzpah).\n\n", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.TotalCount);
            }
            else
            {
                text = string.Format("{0} passed, {1} failed, {2} skipped, {3} total (chutzpah).\n\n", testResultsSummary.PassedCount, testResultsSummary.FailedCount, testResultsSummary.SkippedCount, testResultsSummary.TotalCount);
            }
            testPane.OutputString(text);
        }

        protected override void TestFailed(TestContext context, TestCase result)
        {
            var errorMessage = GetTestFailureMessage(result);
            WriteToOutputPaneAndErrorTaskList(result.InputTestFile, errorMessage, errorMessage, result.Line);
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        protected override void TestPassed(TestContext context, TestCase result)
        {
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        protected override void TestSkipped(TestContext context, TestCase result)
        {
            SetStatusBarMessage(GetStatusBarMessage(result));
        }

        public override void FileError(TestContext context, TestError error)
        {
            testPane.OutputString(GetFileErrorMessage(error));
        }

        public override void FileLog(TestContext context, TestLog log)
        {
            testPane.OutputString(GetFileLogMessage(log));
        }

        public override void ExceptionThrown(Exception exception, string fileName)
        {
            testPane.OutputString(GetExceptionThrownMessage(exception, fileName));
        }

        protected string GetStatusBarMessage(TestCase result)
        {
            var title = result.GetDisplayName();
            var status = result.TestOutcome == TestOutcome.Skipped ? "skipped" : (result.TestOutcome == TestOutcome.Passed ? "passed" : "failed");
            return string.Format("{0} ({1})", title, status);
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