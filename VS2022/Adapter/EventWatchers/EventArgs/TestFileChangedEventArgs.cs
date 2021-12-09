using Chutzpah.VS2022.TestAdapter;

namespace Chutzpah.VS11.EventWatchers.EventArgs
{
    public enum TestFileChangedReason
    {
        None,
        Added,
        Removed,
        Changed,
    }

    public class TestFileChangedEventArgs : System.EventArgs
    {
        public TestFileCandidate File { get; private set; }
        public TestFileChangedReason ChangedReason { get; private set; }

        public TestFileChangedEventArgs(string file, TestFileChangedReason reason)
        {
            File = new TestFileCandidate(file);
            ChangedReason = reason;
        }
    }
}