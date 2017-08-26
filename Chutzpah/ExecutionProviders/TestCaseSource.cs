using System;
using System.Threading.Tasks;

namespace Chutzpah.Models
{
    public abstract class TestCaseSource<T> : IDisposable
    {
        private Func<T, bool> subscriber = null;
        private readonly int timeout;
        public DateTime LastTestEvent { get; set; } = DateTime.Now;

        public TestCaseSource(int timeout)
        {
            this.timeout = timeout;
        }

        public bool IsAlive
        {
            get
            {
                return (DateTime.Now - LastTestEvent).TotalMilliseconds < timeout;
            }
        }


        public void Subscribe(Func<T, bool> handler)
        {
            subscriber = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public abstract Task<object> Open();

        public virtual void Dispose()
        {
        }

        protected void Emit(T data)
        {
            // Always wait on previous task before emitting next
            var wasTestEvent = subscriber(data);
            if (wasTestEvent)
            {
                LastTestEvent = DateTime.Now;
            }

        }
    }
}